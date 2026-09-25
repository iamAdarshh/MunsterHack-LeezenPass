using FastEndpoints;
using LeezenPass.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Stolen.Get;

public sealed record GetStolenRequest
{
  public string Token { get; init; } = string.Empty;
}

/// <summary>Public page of one stolen bike (share card, QR tag). 404 when the bike isn't stolen (right now).</summary>
public class GetStolenEndpoint(AppDbContext db) : Endpoint<GetStolenRequest, StolenBikeResponse>
{
  public override void Configure()
  {
    Get("stolen/{token}");
    AllowAnonymous();
    Summary(s => s.Summary = "Public view of one stolen bike by its public token");
  }

  public override async Task HandleAsync(GetStolenRequest req, CancellationToken ct)
  {
    var row = await db.StolenBikes(new StolenBikeFilter(Token: req.Token)).FirstOrDefaultAsync(ct);
    if (row is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    await Send.OkAsync(row.ToResponse(), ct);
  }
}
