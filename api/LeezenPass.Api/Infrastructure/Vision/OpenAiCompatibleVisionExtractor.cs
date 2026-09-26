using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using LeezenPass.Api.Domain.Verification;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Infrastructure.Vision;

/// <summary>
/// Vision model behind an OpenAI-compatible chat completions API (LM Studio locally, or a cloud provider).
/// Uses structured output (json_schema, strict) so the reply is always our schema.
/// </summary>
public class OpenAiCompatibleVisionExtractor(
  HttpClient http,
  IOptions<VisionOptions> options,
  ILogger<OpenAiCompatibleVisionExtractor> logger) : IVisionExtractor
{
  // A local model handles one image at a time; parallel requests (two phones, two tabs) would both time out.
  // Static: the typed HttpClient service is transient.
  private static readonly SemaphoreSlim Gate = new(1, 1);

  private readonly VisionOptions _options = options.Value;

  public async Task<VisionSuggestion> ExtractAsync(Stream image, string contentType, CancellationToken ct)
  {
    var output = await CompleteAsync<VisionModelOutput>(
      image, contentType, VisionPrompt.System, VisionPrompt.User, "bike", VisionPrompt.Schema(), ct);
    return VisionResponseMapper.Map(output);
  }

  public async Task<ReceiptReading> ExtractReceiptAsync(Stream image, string contentType, VerificationHint hint, CancellationToken ct)
  {
    var output = await CompleteAsync<ReceiptModelOutput>(
      image, contentType, VisionPrompt.ReceiptSystem, VisionPrompt.ReceiptUser, "receipt", VisionPrompt.ReceiptSchema(), ct);
    return new ReceiptReading(
      Blank(output.FrameNumberCandidate),
      Blank(output.Brand),
      Blank(output.Model),
      DateOnly.TryParseExact(output.PurchaseDate, "yyyy-MM-dd", out var date) ? date : null,
      Math.Clamp(output.Confidence ?? 0, 0, 1));
  }

  public async Task<PossessionReading> CheckPossessionAsync(Stream image, string contentType, VerificationHint hint, CancellationToken ct)
  {
    var output = await CompleteAsync<PossessionModelOutput>(
      image, contentType, VisionPrompt.PossessionSystem, VisionPrompt.PossessionUser, "possession", VisionPrompt.PossessionSchema(), ct);
    return new PossessionReading(
      output.CodeVisible ?? false,
      Blank(output.CodeValue),
      Blank(output.FrameNumberCandidate),
      Math.Clamp(output.Confidence ?? 0, 0, 1));
  }

  private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  /// <summary>One image + prompt → strict JSON of <typeparamref name="T"/>. Gate, timeout and error mapping shared by all calls.</summary>
  private async Task<T> CompleteAsync<T>(
    Stream image, string contentType, string system, string user, string schemaName, JsonObject schema, CancellationToken ct)
  {
    using var buffer = new MemoryStream();
    await image.CopyToAsync(buffer, ct);
    var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(buffer.ToArray())}";

    var body = new JsonObject
    {
      ["model"] = _options.Model,
      ["temperature"] = 0,
      ["max_tokens"] = 300,
      ["messages"] = new JsonArray
      {
        new JsonObject { ["role"] = "system", ["content"] = system },
        new JsonObject
        {
          ["role"] = "user",
          ["content"] = new JsonArray
          {
            new JsonObject { ["type"] = "text", ["text"] = user },
            new JsonObject { ["type"] = "image_url", ["image_url"] = new JsonObject { ["url"] = dataUrl } },
          },
        },
      },
      ["response_format"] = new JsonObject
      {
        ["type"] = "json_schema",
        ["json_schema"] = new JsonObject { ["name"] = schemaName, ["strict"] = true, ["schema"] = schema },
      },
    };

    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

    var entered = false;
    try
    {
      // Waiting in line counts toward the timeout: the user never waits longer than TimeoutSeconds.
      await Gate.WaitAsync(timeout.Token);
      entered = true;

      using var request = CreateRequest(body);
      using var response = await http.SendAsync(request, timeout.Token);
      if (!response.IsSuccessStatusCode)
      {
        throw new VisionUnavailableException($"Vision API returned {(int)response.StatusCode}.");
      }

      var completion = await response.Content.ReadFromJsonAsync<JsonObject>(timeout.Token);
      var content = completion?["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
        ?? throw new VisionUnavailableException("Vision API returned no content.");

      return JsonSerializer.Deserialize<T>(content)
        ?? throw new VisionUnavailableException("Vision API returned empty JSON.");
    }
    catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
    {
      throw new VisionUnavailableException($"Vision API took longer than {_options.TimeoutSeconds}s.", ex);
    }
    catch (Exception ex) when (ex is not OperationCanceledException and not VisionUnavailableException)
    {
      // Any other failure (connection refused, bad JSON, misconfigured BaseUrl, ...) -> manual entry, never a 500.
      throw new VisionUnavailableException($"Vision API failed: {ex.GetType().Name}.", ex);
    }
    finally
    {
      if (entered)
      {
        Gate.Release();
      }
    }
  }

  /// <summary>Tiny text-only request so the model is loaded before the first real photo (cold load is ~16 s).</summary>
  public async Task WarmUpAsync(CancellationToken ct)
  {
    var body = new JsonObject
    {
      ["model"] = _options.Model,
      ["max_tokens"] = 1,
      ["messages"] = new JsonArray { new JsonObject { ["role"] = "user", ["content"] = "ok" } },
    };

    try
    {
      using var request = CreateRequest(body);
      using var response = await http.SendAsync(request, ct);
      logger.LogInformation("Vision model {Model} warm-up: {Status}", _options.Model, (int)response.StatusCode);
    }
    catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
    {
      // Best effort: a bad BaseUrl or a stopped LM Studio must never keep the API from starting.
      logger.LogWarning("Vision model not reachable at {BaseUrl} ({Error}); AI prefill will fall back to manual entry.",
        _options.BaseUrl, ex.GetType().Name);
    }
  }

  private HttpRequestMessage CreateRequest(JsonObject body)
  {
    var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
    {
      Content = JsonContent.Create(body),
    };
    if (!string.IsNullOrWhiteSpace(_options.ApiKey))
    {
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    return request;
  }
}

/// <summary>Warms the model up in the background at startup; never blocks or fails startup.</summary>
public class VisionWarmupService(IServiceProvider services) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await using var scope = services.CreateAsyncScope();
    if (scope.ServiceProvider.GetRequiredService<IVisionExtractor>() is OpenAiCompatibleVisionExtractor extractor)
    {
      using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
      timeout.CancelAfter(TimeSpan.FromSeconds(60));
      await extractor.WarmUpAsync(timeout.Token);
    }
  }
}
