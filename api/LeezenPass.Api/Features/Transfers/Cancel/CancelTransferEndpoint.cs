using FastEndpoints;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Transfers.Cancel;

public sealed record CancelTransferRequest
{
  public Guid Id { get; init; }
}

/// <summary>Seller withdraws the open code (e.g. the sale fell through).</summary>
public class CancelTransferEndpoint(AppDbContext db, IClock clock) : Endpoint<CancelTransferRequest>
{
  public override void Configure()
  {
    Delete("bikes/{id}/transfers");
    Summary(s => s.Summary = "Cancel the open transfer code");
  }

  public override async Task HandleAsync(CancelTransferRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    if (!await db.Bikes.AnyAsync(b => b.Id == req.Id && b.OwnerId == userId, ct))
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var now = clock.UtcNow;
    foreach (var open in await db.OwnershipTransfers.Where(t => t.BikeId == req.Id).OpenAt(now).ToListAsync(ct))
    {
      open.Cancel(now);
    }

    await db.SaveChangesAsync(ct);
    await Send.NoContentAsync(ct);
  }
}
