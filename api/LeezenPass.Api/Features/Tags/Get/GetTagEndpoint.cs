using FastEndpoints;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Tags.Get;

public sealed record GetTagRequest
{
  public string Token { get; init; } = string.Empty;
}

/// <summary>
/// "registered" or "stolen", whether the owner has prepared a handover (open transfer code), and the
/// frame number hint to compare with the bike. No attributes, no owner data.
/// </summary>
public sealed record TagResponse(string Status, bool TransferOpen, string FrameNumberHint, TrustLevel TrustLevel);

/// <summary>
/// Data for the QR tag page (/b/{token}). 404 for unknown tokens, so a fake QR code sticker
/// can't make a stolen bike look "registered and clean".
/// </summary>
public class GetTagEndpoint(AppDbContext db, IClock clock) : Endpoint<GetTagRequest, TagResponse>
{
  public override void Configure()
  {
    Get("tags/{token}");
    AllowAnonymous();
    Summary(s => s.Summary = "Status behind a QR tag: registered | stolen");
  }

  public override async Task HandleAsync(GetTagRequest req, CancellationToken ct)
  {
    var now = clock.UtcNow;
    var bike = await db.Bikes
      .Where(b => b.PublicToken == req.Token)
      .Select(b => new
      {
        b.Status,
        b.FrameNoNorm,
        b.TrustLevel,
        TransferOpen = db.OwnershipTransfers.Any(t => t.BikeId == b.Id && t.CompletedAt == null && t.ExpiresAt > now),
      })
      .FirstOrDefaultAsync(ct);

    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var stolen = bike.Status == BikeStatus.Stolen;
    await Send.OkAsync(new TagResponse(stolen ? "stolen" : "registered", !stolen && bike.TransferOpen, FrameNumber.Hint(bike.FrameNoNorm), bike.TrustLevel), ct);
  }
}
