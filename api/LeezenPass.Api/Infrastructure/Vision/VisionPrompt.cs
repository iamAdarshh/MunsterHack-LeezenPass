using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Infrastructure.Vision;

/// <summary>Prompt + strict JSON schema (SPEC "AI extraction"). Enum values are exactly our catalog keys.</summary>
public static class VisionPrompt
{
  public const string NoColor = "none";

  public const string System =
    "You describe bicycles in photos for a bike registry. Only report what is clearly visible. "
    + "Types: city = upright city/Dutch/roadster bike with swept-back handlebars; trekking = touring bike with rack and lights; "
    + "road = racing bike with drop handlebars; mountain = knobby tyres, flat bar, often suspension; gravel = drop bars with wide tyres; "
    + "cargo = box or long cargo bike; folding = small-wheel folding bike; kids = child size. "
    + "brand_guess: brand name if readable on the frame, else empty string. "
    + "frame_number_candidate: the stamped frame/serial number exactly as printed if readable, else empty string. "
    + "color_secondary: 'none' if there is no clear second colour. "
    + "confidence: 0-1, how sure you are that this is a bicycle and the description is right.";

  public const string User = "Describe this bicycle.";

  public static JsonObject Schema()
  {
    static JsonArray Strings(IEnumerable<string> values) => new(values.Select(v => (JsonNode)v).ToArray());

    return new JsonObject
    {
      ["type"] = "object",
      ["additionalProperties"] = false,
      ["required"] = Strings(["type", "color_primary", "color_secondary", "brand_guess", "features", "frame_number_candidate", "confidence"]),
      ["properties"] = new JsonObject
      {
        ["type"] = new JsonObject { ["type"] = "string", ["enum"] = Strings(Enum.GetValues<BikeType>().Select(TypeKey)) },
        ["color_primary"] = new JsonObject { ["type"] = "string", ["enum"] = Strings(BikeCatalog.Colors.Order()) },
        ["color_secondary"] = new JsonObject { ["type"] = "string", ["enum"] = Strings(BikeCatalog.Colors.Order().Append(NoColor)) },
        ["brand_guess"] = new JsonObject { ["type"] = "string" },
        ["features"] = new JsonObject
        {
          ["type"] = "array",
          ["items"] = new JsonObject { ["type"] = "string", ["enum"] = Strings(BikeCatalog.Features.Order()) },
        },
        ["frame_number_candidate"] = new JsonObject { ["type"] = "string" },
        ["confidence"] = new JsonObject { ["type"] = "number", ["minimum"] = 0, ["maximum"] = 1 },
      },
    };
  }

  public static string TypeKey(BikeType type) => JsonNamingPolicy.SnakeCaseLower.ConvertName(type.ToString());
}

/// <summary>Raw model output, exactly as the schema describes it. Never trusted as-is: see <see cref="VisionResponseMapper"/>.</summary>
public sealed record VisionModelOutput(
  [property: JsonPropertyName("type")] string? Type,
  [property: JsonPropertyName("color_primary")] string? ColorPrimary,
  [property: JsonPropertyName("color_secondary")] string? ColorSecondary,
  [property: JsonPropertyName("brand_guess")] string? BrandGuess,
  [property: JsonPropertyName("features")] List<string>? Features,
  [property: JsonPropertyName("frame_number_candidate")] string? FrameNumberCandidate,
  [property: JsonPropertyName("confidence")] double? Confidence);
