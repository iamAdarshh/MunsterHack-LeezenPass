namespace LeezenPass.Api.Features.Health;

public sealed record HealthResponse(string Status, bool Database, bool DemoMode, bool UseFakes);
