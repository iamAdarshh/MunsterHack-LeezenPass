using LeezenPass.Api.Domain.Bikes;

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
}
