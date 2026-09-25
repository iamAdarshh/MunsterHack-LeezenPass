using FastEndpoints;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Transfers.Status;

public sealed record GetTransferStatusRequest
{
  public Guid Id { get; init; }
}

public sealed record OpenTransferResponse(Guid Id, DateTimeOffset ExpiresAt);

/// <param name="OpenTransfer">Code waiting for a buyer (the code itself can't be shown again).</param>
/// <param name="CertificateTransferId">Transfer that made the current owner the owner (certificate PDF).</param>
/// <param name="PreviousOwners">Number of earlier owners; no identities.</param>
public sealed record TransferStatusResponse(OpenTransferResponse? OpenTransfer, Guid? CertificateTransferId, int PreviousOwners);

/// <summary>Transfer state of one of the signed-in user's bikes.</summary>
public class GetTransferStatusEndpoint(AppDbContext db, IClock clock) : Endpoint<GetTransferStatusRequest, TransferStatusResponse>
{
  public override void Configure()
  {
    Get("bikes/{id}/transfers");
    Summary(s => s.Summary = "Transfer state: open code, certificate, number of previous owners");
  }

  public override async Task HandleAsync(GetTransferStatusRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    if (!await db.Bikes.AnyAsync(b => b.Id == req.Id && b.OwnerId == userId, ct))
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var open = await db.OwnershipTransfers
      .Where(t => t.BikeId == req.Id)
      .OpenAt(clock.UtcNow)
      .OrderByDescending(t => t.CreatedAt)
      .Select(t => new OpenTransferResponse(t.Id, t.ExpiresAt))
      .FirstOrDefaultAsync(ct);

    var certificate = await db.OwnershipTransfers
      .Where(t => t.BikeId == req.Id && t.ToUserId == userId && t.CompletedAt != null)
      .OrderByDescending(t => t.CompletedAt)
      .Select(t => (Guid?)t.Id)
      .FirstOrDefaultAsync(ct);

    var owners = await db.BikeOwnershipHistory.CountAsync(h => h.BikeId == req.Id, ct);

    await Send.OkAsync(new TransferStatusResponse(open, certificate, Math.Max(0, owners - 1)), ct);
  }
}
