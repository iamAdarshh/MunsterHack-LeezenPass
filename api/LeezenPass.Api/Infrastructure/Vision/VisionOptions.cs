namespace LeezenPass.Api.Infrastructure.Vision;

public enum VisionProvider
{
  /// <summary>Fake when Features:UseFakes is on, otherwise OpenAiCompatible.</summary>
  Auto,
  Fake,

  /// <summary>Any OpenAI-compatible chat API with image input: LM Studio (local), OpenAI, ...</summary>
  OpenAiCompatible,
}

public class VisionOptions
{
  public const string Section = "Vision";

  public VisionProvider Provider { get; set; } = VisionProvider.Auto;

  /// <summary>e.g. http://localhost:1234/v1 for LM Studio.</summary>
  public string BaseUrl { get; set; } = "http://localhost:1234/v1";

  /// <summary>Not needed for LM Studio.</summary>
  public string? ApiKey { get; set; }

  public string Model { get; set; } = "qwen/qwen3-vl-8b";

  /// <summary>After this the UI falls back to manual entry.</summary>
  public int TimeoutSeconds { get; set; } = 15;

  /// <summary>Images are downscaled before sending: faster, and enough for type/colour/OCR.</summary>
  public int MaxImageEdge { get; set; } = 1024;
}
