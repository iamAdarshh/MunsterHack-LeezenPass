using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Infrastructure.Vision;

/// <summary>Suggests bike attributes from a photo. Suggestions only: the user confirms, never auto-save.</summary>
public interface IVisionExtractor
{
  Task<VisionSuggestion> ExtractAsync(Stream image, string contentType, CancellationToken ct);
}

/// <summary>Mirrors the strict JSON schema in SPEC "AI extraction". Colours and features are i18n keys.</summary>
public sealed record VisionSuggestion(
  BikeType? Type,
  string? ColorPrimary,
  string? ColorSecondary,
  string? BrandGuess,
  IReadOnlyList<string> Features,
  string? FrameNumberCandidate,
  double Confidence);
