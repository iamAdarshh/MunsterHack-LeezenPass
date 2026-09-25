namespace LeezenPass.Api.Infrastructure.Vision;

/// <summary>Model not reachable, too slow or returned garbage. Callers fall back to manual entry.</summary>
public class VisionUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
