using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Features.Bikes.Get;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Bikes.Register;

public class RegisterBikeEndpoint(AppDbContext db, IClock clock, IOptions<FeinOptions> fein)
  : Endpoint<RegisterBikeRequest, BikeResponse>
{
  public override void Configure()
  {
    Post("bikes");
    Options(x => x.RequireRateLimiting(RateLimitPolicies.BikeWrite));
    Summary(s => s.Summary = "Register a bike for the signed-in user");
  }

  public override async Task HandleAsync(RegisterBikeRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var frameNumber = FrameNumber.Create(req.FrameNumber);

    // Says only "already registered", nothing about the owner.
    if (await db.Bikes.AnyAsync(b => b.FrameNoNorm == frameNumber.Normalized, ct))
    {
      await SendFrameNumberTaken(ct);
      return;
    }

    var now = clock.UtcNow;
    var bike = new Bike(userId, frameNumber, now);
    bike.UpdateDetails(req.ToDetails());
    if (FeinCode.TryCreate(req.FeinCode, out var feinCode))
    {
      bike.SetFeinCode(feinCode, fein.Value.SecretBytes());
    }

    db.Bikes.Add(bike);
    db.BikeOwnershipHistory.Add(new BikeOwnershipHistory(bike.Id, userId, now));

    try
    {
      await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (ex.IsUniqueViolation())
    {
      await SendFrameNumberTaken(ct);
      return;
    }

    await Send.CreatedAtAsync<GetBikeEndpoint>(new { id = bike.Id }, BikeResponse.From(bike), cancellation: ct);
  }

  private Task SendFrameNumberTaken(CancellationToken ct)
  {
    AddError(r => r.FrameNumber, "errors.frameNumberTaken");
    return Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
  }
}
