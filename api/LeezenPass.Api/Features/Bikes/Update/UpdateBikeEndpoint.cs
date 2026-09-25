using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Bikes.Update;

public class UpdateBikeEndpoint(AppDbContext db, IOptions<FeinOptions> fein) : Endpoint<UpdateBikeRequest, BikeResponse>
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
      .Include(b => b.Photos)
      .FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var frameNumber = FrameNumber.Create(req.FrameNumber);
    if (frameNumber.Normalized != bike.FrameNoNorm &&
        await db.Bikes.AnyAsync(b => b.FrameNoNorm == frameNumber.Normalized, ct))
    {
      await SendFrameNumberTaken(ct);
      return;
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
      await SendFrameNumberTaken(ct);
      return;
    }

    await Send.OkAsync(BikeResponse.From(bike), ct);
  }

  private Task SendFrameNumberTaken(CancellationToken ct)
  {
    AddError(r => r.FrameNumber, "errors.frameNumberTaken");
    return Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
  }
}
