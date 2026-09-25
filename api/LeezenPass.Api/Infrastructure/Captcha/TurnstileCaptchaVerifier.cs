using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Infrastructure.Captcha;

/// <summary>Cloudflare Turnstile. Fails closed: network errors count as "not verified".</summary>
public class TurnstileCaptchaVerifier(
  HttpClient http,
  IOptions<CaptchaOptions> options,
  ILogger<TurnstileCaptchaVerifier> logger) : ICaptchaVerifier
{
  private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

  public async Task<bool> VerifyAsync(string? token, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(token))
    {
      return false;
    }

    try
    {
      using var content = new FormUrlEncodedContent(new Dictionary<string, string>
      {
        ["secret"] = options.Value.SecretKey,
        ["response"] = token,
      });
      using var response = await http.PostAsync(VerifyUrl, content, ct);
      var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>(ct);
      return result?.Success == true;
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
    {
      logger.LogWarning("Captcha verification failed: {Error}", ex.GetType().Name);
      return false;
    }
  }

  private sealed record TurnstileResponse(bool Success);
}
