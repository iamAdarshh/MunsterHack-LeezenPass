using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Infrastructure.Vision;

/// <summary>
/// Turns model output into a safe suggestion: unknown keys dropped, empty strings to null,
/// frame number only if it is a plausible frame number, confidence clamped to 0..1.
/// </summary>
public static class VisionResponseMapper
{
  private const int MaxBrandLength = 60;

  public static VisionSuggestion Map(VisionModelOutput output)
  {
    var type = Enum.GetValues<BikeType>().Cast<BikeType?>()
      .FirstOrDefault(t => VisionPrompt.TypeKey(t!.Value) == output.Type?.Trim().ToLowerInvariant());

    var primary = Color(output.ColorPrimary);
    var secondary = Color(output.ColorSecondary);
    if (secondary == primary)
    {
      secondary = null;
    }

    var brand = Clean(output.BrandGuess);
    if (brand is { Length: > MaxBrandLength })
    {
      brand = brand[..MaxBrandLength];
    }

    // Real frame numbers contain digits; this drops model answers like "unreadable" or "UNKNOWN".
    var frame = Clean(output.FrameNumberCandidate);
    if (!FrameNumber.TryCreate(frame, out _) || !frame!.Any(char.IsAsciiDigit))
    {
      frame = null;
    }

    var features = (output.Features ?? [])
      .Select(f => f.Trim().ToLowerInvariant())
      .Where(BikeCatalog.Features.Contains)
      .Distinct()
      .ToList();

    var confidence = output.Confidence is { } c && double.IsFinite(c) ? Math.Clamp(c, 0, 1) : 0;

    return new VisionSuggestion(type, primary, secondary, brand, features, frame, confidence);
  }

  private static string? Color(string? value)
  {
    var key = value?.Trim().ToLowerInvariant();
    return key is not null && BikeCatalog.Colors.Contains(key) ? key : null;
  }

  private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
