using FastEndpoints;
using FluentValidation;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Transfers;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Storage;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Transfers.Claim;

public sealed record ClaimTransferRequest
{
  public string Code { get; init; } = string.Empty;
}

public sealed record ClaimTransferResponse(Guid BikeId, Guid TransferId);

public class ClaimTransferValidator : Validator<ClaimTransferRequest>
{
  public ClaimTransferValidator()
  {
    RuleFor(x => x.Code).Must(c => TransferCode.TryParse(c, out _)).WithMessage("errors.transferCodeInvalid");
  }
}

/// <summary>
/// Buyer enters the code: the bike moves to the buyer, the history is kept.
/// Unknown, expired and used codes get the same answer, so guessing learns nothing.
/// </summary>
public class ClaimTransferEndpoint(AppDbContext db, IClock clock, IFileStorage storage, ILogger<ClaimTransferEndpoint> logger)
  : Endpoint<ClaimTransferRequest, ClaimTransferResponse>
{
  public override void Configure()
  {
    Post("transfers/claim");
    Options(x => x.RequireRateLimiting(RateLimitPolicies.Claim));
    Summary(s => s.Summary = "Claim a bike with a transfer code");
  }

  public override async Task HandleAsync(ClaimTransferRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var now = clock.UtcNow;
    TransferCode.TryParse(req.Code, out var code);
    var codeHash = code!.ComputeHash();

    var transfer = await db.OwnershipTransfers.FirstOrDefaultAsync(t => t.CodeHash == codeHash, ct);
    if (transfer is null || !transfer.IsOpenAt(now))
    {
      await SendError("errors.transferCodeInvalid", StatusCodes.Status400BadRequest, ct);
      return;
    }

    if (transfer.FromUserId == userId)
    {
      await SendError("errors.transferOwnBike", StatusCodes.Status400BadRequest, ct);
      return;
    }

    var bike = await db.Bikes.Include(b => b.OwnershipHistory).Include(b => b.Photos).AsSplitQuery()
      .FirstAsync(b => b.Id == transfer.BikeId, ct);
    if (bike.OwnerId != transfer.FromUserId)
    {
      // The seller no longer owns the bike: the code is worthless.
      await SendError("errors.transferCodeInvalid", StatusCodes.Status400BadRequest, ct);
      return;
    }

    if (!bike.CanTransfer)
    {
      await SendError("errors.transferStolen", StatusCodes.Status409Conflict, ct);
      return;
    }

    transfer.Complete(userId, now);
    var removedPhotos = bike.TransferTo(userId, now);
    db.BikePhotos.RemoveRange(removedPhotos);

    try
    {
      await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException ex)
    {
      // Someone else claimed the same code a moment earlier.
      logger.LogWarning(ex, "Transfer {TransferId} was claimed concurrently", transfer.Id);
      await SendError("errors.transferCodeInvalid", StatusCodes.Status400BadRequest, ct);
      return;
    }

    // Seller's receipt photos: files go after the DB commit (a leftover file is harmless).
    foreach (var photo in removedPhotos)
    {
      try
      {
        await storage.DeleteAsync(photo.Path, CancellationToken.None);
        await storage.DeleteAsync(PhotoStorageKeys.Thumbnail(photo.Path), CancellationToken.None);
      }
      catch (IOException ex)
      {
        logger.LogWarning("Could not delete receipt photo file: {Error}", ex.GetType().Name);
      }
    }

    await Send.OkAsync(new ClaimTransferResponse(bike.Id, transfer.Id), ct);
  }

  private Task SendError(string key, int status, CancellationToken ct)
  {
    AddError(r => r.Code, key);
    return Send.ErrorsAsync(status, ct);
  }
}
