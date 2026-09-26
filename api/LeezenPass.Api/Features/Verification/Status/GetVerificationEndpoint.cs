using FastEndpoints;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Verification.Status;

public sealed record GetVerificationRequest
{
  public Guid Id { get; init; }
}

public class GetVerificationEndpoint(AppDbContext db, IClock clock) : Endpoint<GetVerificationRequest, VerificationStatusResponse>
{
  public override void Configure()
  {
    Get("bikes/{id}/verification");
    Summary(s => s.Summary = "Trust level and receipt/possession check status");
  }

  public override async Task HandleAsync(GetVerificationRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var bike = await db.Bikes.AsNoTracking().FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    await Send.OkAsync(await db.StatusAsync(bike, userId, clock.UtcNow, ct), ct);
  }
}
