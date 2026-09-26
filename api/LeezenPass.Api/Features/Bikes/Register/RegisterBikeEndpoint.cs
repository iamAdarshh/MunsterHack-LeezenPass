using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Features.Bikes.Get;
using LeezenPass.Api.Infrastructure;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Bikes.Register;

/// <summary>
/// Registers a bike. An existing frame number always gets the same 409, whether that bike is stolen or not
/// (see <see cref="FrameConflicts"/>).
/// </summary>
public class RegisterBikeEndpoint(AppDbContext db, IClock clock, IOptions<FeinOptions> fein, FrameConflicts conflicts)
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
    if (conflicts.LimitReached(userId))
    {
      AddError("errors.rateLimited");
      await Send.ErrorsAsync(StatusCodes.Status429TooManyRequests, ct);
      return;
    }

    var frameNumber = FrameNumber.Create(req.FrameNumber);
    var existing = await FrameConflicts.FindAsync(db, frameNumber.Normalized, ct);
    if (existing is not null)
    {
      await SendFrameNumberTaken(existing, userId, ct);
      return;
    }

    var bike = new Bike(userId, frameNumber, clock.UtcNow);
    bike.UpdateDetails(req.ToDetails());
    if (FeinCode.TryCreate(req.FeinCode, out var feinCode))
    {
      bike.SetFeinCode(feinCode, fein.Value.SecretBytes());
    }

    db.Bikes.Add(bike);

    try
    {
      await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (ex.IsUniqueViolation())
    {
      // Registered by someone else in the same moment: same answer as above.
      await SendFrameNumberTaken(null, userId, ct);
      return;
    }

    await Send.CreatedAtAsync<GetBikeEndpoint>(new { id = bike.Id }, BikeResponse.From(bike), cancellation: ct);
  }

  private Task SendFrameNumberTaken(ExistingBike? existing, Guid userId, CancellationToken ct)
  {
    conflicts.Record(existing, userId, ClientKey.For(HttpContext.Connection.RemoteIpAddress));
    AddError(r => r.FrameNumber, "errors.frameNumberTaken");
    return Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
  }
}
