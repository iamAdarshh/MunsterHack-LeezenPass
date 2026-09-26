using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Verification;

namespace LeezenPass.Api.Infrastructure.Vision;

/// <summary>Canned suggestion so the demo works offline. The real provider is chosen at the event.</summary>
public class FakeVisionExtractor : IVisionExtractor
{
  public async Task<VisionSuggestion> ExtractAsync(Stream image, string contentType, CancellationToken ct)
  {
    // Feels like a real call in the UI (loading state is visible).
    await Task.Delay(TimeSpan.FromMilliseconds(800), ct);

    return new VisionSuggestion(
      Type: BikeType.Trekking,
      ColorPrimary: "black",
      ColorSecondary: "silver",
      BrandGuess: "Gazelle",
      Features: ["rack", "fenders", "hub_dynamo", "kickstand"],
      FrameNumberCandidate: "WGZ 1234 5678",
      Confidence: 0.82);
  }

  public async Task<ReceiptReading> ExtractReceiptAsync(Stream image, string contentType, VerificationHint hint, CancellationToken ct)
  {
    await Task.Delay(TimeSpan.FromMilliseconds(800), ct);
    return Passes(hint)
      ? new ReceiptReading(hint.FrameNumber, hint.Brand, hint.Model, new DateOnly(2026, 6, 12), 0.9)
      : new ReceiptReading(null, "Wochenmarkt", null, new DateOnly(2026, 6, 12), 0.9);
  }

  public async Task<PossessionReading> CheckPossessionAsync(Stream image, string contentType, VerificationHint hint, CancellationToken ct)
  {
    await Task.Delay(TimeSpan.FromMilliseconds(800), ct);
    return Passes(hint)
      ? new PossessionReading(true, hint.ExpectedCode, hint.FrameNumber, 0.9)
      : new PossessionReading(true, "0000", "WGA 1234 5678", 0.9);
  }

  /// <summary>Demo script: sample photos seed/photos/*_pass.jpg pass, *_fail.jpg fail.</summary>
  private static bool Passes(VerificationHint hint) => hint.FileName.Contains("pass", StringComparison.OrdinalIgnoreCase);
}
