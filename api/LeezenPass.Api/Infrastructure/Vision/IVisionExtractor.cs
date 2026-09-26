using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Verification;

namespace LeezenPass.Api.Infrastructure.Vision;

/// <summary>Suggests bike attributes from a photo. Suggestions only: the user confirms, never auto-save.</summary>
public interface IVisionExtractor
{
  Task<VisionSuggestion> ExtractAsync(Stream image, string contentType, CancellationToken ct);

  /// <summary>Reads frame number, brand, model and date from a sales receipt (ownership check).</summary>
  Task<ReceiptReading> ExtractReceiptAsync(Stream image, string contentType, VerificationHint hint, CancellationToken ct);

  /// <summary>Reads the handwritten challenge code and the frame number from a possession photo.</summary>
  Task<PossessionReading> CheckPossessionAsync(Stream image, string contentType, VerificationHint hint, CancellationToken ct);
}

/// <summary>
/// Only for <see cref="FakeVisionExtractor"/> (demo script: a file name containing "pass" passes, anything else fails).
/// Real providers ignore it and read the photo.
/// </summary>
public sealed record VerificationHint(string FileName, string FrameNumber, string? Brand, string? Model, string? ExpectedCode);

/// <summary>Mirrors the strict JSON schema in SPEC "AI extraction". Colours and features are i18n keys.</summary>
public sealed record VisionSuggestion(
  BikeType? Type,
  string? ColorPrimary,
  string? ColorSecondary,
  string? BrandGuess,
  IReadOnlyList<string> Features,
  string? FrameNumberCandidate,
  double Confidence);
