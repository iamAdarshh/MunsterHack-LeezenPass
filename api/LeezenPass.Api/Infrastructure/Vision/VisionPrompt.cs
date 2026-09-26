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

  public const string ReceiptSystem =
    "You read sales receipts for bicycles. Only report what is printed on the receipt. "
    + "frame_number_candidate: the frame/serial number (Rahmennummer) exactly as printed, else empty string. "
    + "brand, model: of the bicycle sold, else empty string. purchase_date: YYYY-MM-DD, else empty string. "
    + "shop_name: the seller, else empty string. "
    + "If it is not a bicycle receipt, leave the bicycle fields empty. "
    + "confidence: 0-1, how sure you are that you read the receipt correctly (a clear non-bicycle receipt is still high).";

  public const string ReceiptUser = "Read this receipt.";

  public const string PossessionSystem =
    "The photo should show a stamped bicycle frame number and, next to it, a handwritten or displayed 4-digit code. "
    + "code_visible: true if a 4-digit code is clearly readable. code_value: the 4 digits, else empty string. "
    + "frame_number_candidate: the stamped frame number exactly as shown, else empty string. "
    + "confidence: 0-1, how sure you are about both readings.";

  public const string PossessionUser = "Read the code and the frame number.";

  public static JsonObject ReceiptSchema() => StrictObject(new JsonObject
  {
    ["frame_number_candidate"] = new JsonObject { ["type"] = "string" },
    ["brand"] = new JsonObject { ["type"] = "string" },
    ["model"] = new JsonObject { ["type"] = "string" },
    ["purchase_date"] = new JsonObject { ["type"] = "string" },
    ["shop_name"] = new JsonObject { ["type"] = "string" },
    ["confidence"] = new JsonObject { ["type"] = "number", ["minimum"] = 0, ["maximum"] = 1 },
  });

  public static JsonObject PossessionSchema() => StrictObject(new JsonObject
  {
    ["code_visible"] = new JsonObject { ["type"] = "boolean" },
    ["code_value"] = new JsonObject { ["type"] = "string" },
    ["frame_number_candidate"] = new JsonObject { ["type"] = "string" },
    ["confidence"] = new JsonObject { ["type"] = "number", ["minimum"] = 0, ["maximum"] = 1 },
  });

  private static JsonObject StrictObject(JsonObject properties) => new()
  {
    ["type"] = "object",
    ["additionalProperties"] = false,
    ["required"] = new JsonArray(properties.Select(p => (JsonNode)p.Key).ToArray()),
    ["properties"] = properties,
  };

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

public sealed record ReceiptModelOutput(
  [property: JsonPropertyName("frame_number_candidate")] string? FrameNumberCandidate,
  [property: JsonPropertyName("brand")] string? Brand,
  [property: JsonPropertyName("model")] string? Model,
  [property: JsonPropertyName("purchase_date")] string? PurchaseDate,
  [property: JsonPropertyName("shop_name")] string? ShopName,
  [property: JsonPropertyName("confidence")] double? Confidence);

public sealed record PossessionModelOutput(
  [property: JsonPropertyName("code_visible")] bool? CodeVisible,
  [property: JsonPropertyName("code_value")] string? CodeValue,
  [property: JsonPropertyName("frame_number_candidate")] string? FrameNumberCandidate,
  [property: JsonPropertyName("confidence")] double? Confidence);
