using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Transfers;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Transfers.Create;

public sealed record CreateTransferRequest
{
  public Guid Id { get; init; }
}

/// <summary>The plain code is in this response only. It is stored as a hash and cannot be shown again.</summary>
public sealed record CreateTransferResponse(Guid TransferId, string Code, DateTimeOffset ExpiresAt);

/// <summary>Seller creates a one-time handover code. A new code replaces any open one.</summary>
public class CreateTransferEndpoint(AppDbContext db, IClock clock) : Endpoint<CreateTransferRequest, CreateTransferResponse>
{
  public override void Configure()
  {
    Post("bikes/{id}/transfers");
    Options(x => x.RequireRateLimiting(RateLimitPolicies.BikeWrite));
    Summary(s => s.Summary = "Create a transfer code (8 chars, 48 h, single use)");
  }

  public override async Task HandleAsync(CreateTransferRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var bike = await db.Bikes.FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    if (!bike.CanTransfer)
    {
      AddError("errors.transferStolen");
      await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
      return;
    }

    var now = clock.UtcNow;
    foreach (var open in await db.OwnershipTransfers.Where(t => t.BikeId == bike.Id).OpenAt(now).ToListAsync(ct))
    {
      open.Cancel(now);
    }

    var code = TransferCode.Generate();
    var transfer = new OwnershipTransfer(bike.Id, userId, code, now);
    db.OwnershipTransfers.Add(transfer);
    await db.SaveChangesAsync(ct);

    HttpContext.Response.Headers.CacheControl = "no-store";
    await Send.ResponseAsync(
      new CreateTransferResponse(transfer.Id, TransferQueries.Format(code), transfer.ExpiresAt),
      StatusCodes.Status201Created,
      ct);
  }
}
