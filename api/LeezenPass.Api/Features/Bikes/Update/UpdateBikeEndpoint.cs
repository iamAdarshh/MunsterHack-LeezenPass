using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Bikes.Update;

/// <summary>Updates a bike. A new frame number goes through the same conflict handling as registration.</summary>
public class UpdateBikeEndpoint(AppDbContext db, IOptions<FeinOptions> fein, FrameConflicts conflicts)
  : Endpoint<UpdateBikeRequest, BikeResponse>
{
  public override void Configure()
  {
    Put("bikes/{id}");
    Options(x => x.RequireRateLimiting(RateLimitPolicies.BikeWrite));
    Summary(s => s.Summary = "Update one of the signed-in user's bikes");
  }

  public override async Task HandleAsync(UpdateBikeRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var bike = await db.Bikes
      .WithDetails()
      .FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var frameNumber = FrameNumber.Create(req.FrameNumber);
    var frameChanged = frameNumber.Normalized != bike.FrameNoNorm;
    if (frameChanged)
    {
      if (!bike.CanChangeFrameNumber)
      {
        // The receipt/possession checks proved this frame number; a different one would inherit the label.
        AddError(r => r.FrameNumber, "errors.frameNumberLocked");
        await Send.ErrorsAsync(cancellation: ct);
        return;
      }

      if (conflicts.LimitReached(userId))
      {
        AddError("errors.rateLimited");
        await Send.ErrorsAsync(StatusCodes.Status429TooManyRequests, ct);
        return;
      }

      var existing = await FrameConflicts.FindAsync(db, frameNumber.Normalized, ct);
      if (existing is not null)
      {
        await SendFrameNumberTaken(existing, userId, ct);
        return;
      }

      // Checks passed so far were for the old frame number.
      await db.OwnershipEvidence.Where(e => e.BikeId == bike.Id).ExecuteDeleteAsync(ct);
      await db.PossessionChallenges.Where(c => c.BikeId == bike.Id).ExecuteDeleteAsync(ct);
    }

    bike.SetFrameNumber(frameNumber);
    bike.UpdateDetails(req.ToDetails());
    if (req.RemoveFeinCode)
    {
      bike.ClearFeinCode();
    }
    else if (FeinCode.TryCreate(req.FeinCode, out var feinCode))
    {
      bike.SetFeinCode(feinCode, fein.Value.SecretBytes());
    }

    try
    {
      await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (ex.IsUniqueViolation())
    {
      await SendFrameNumberTaken(null, userId, ct);
      return;
    }

    await Send.OkAsync(BikeResponse.From(bike), ct);
  }

  private Task SendFrameNumberTaken(ExistingBike? existing, Guid userId, CancellationToken ct)
  {
    conflicts.Record(existing, userId, ClientKey.For(HttpContext.Connection.RemoteIpAddress));
    AddError(r => r.FrameNumber, "errors.frameNumberTaken");
    return Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
  }
}
