using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Infrastructure.Data;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Health;

/// <summary>Liveness + DB check. The web app also reads DemoMode from here to show the "Demo-Daten" banner.</summary>
public class GetHealthEndpoint(AppDbContext db, IOptions<FeaturesOptions> features) : EndpointWithoutRequest<HealthResponse>
{
  public override void Configure()
  {
    Get("health");
    AllowAnonymous();
    Summary(s => s.Summary = "Health check");
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var dbOk = await db.Database.CanConnectAsync(ct);
    var response = new HealthResponse(dbOk ? "ok" : "degraded", dbOk, features.Value.DemoMode, features.Value.UseFakes);

    await Send.ResponseAsync(response, dbOk ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable, ct);
  }
}
