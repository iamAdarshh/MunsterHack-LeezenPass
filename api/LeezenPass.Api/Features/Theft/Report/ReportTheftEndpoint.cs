using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Features.Bikes;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Theft.Report;

/// <summary>Owner reports the bike stolen. It then shows up on the public stolen list (district only).</summary>
public class ReportTheftEndpoint(AppDbContext db, IClock clock) : Endpoint<ReportTheftRequest, BikeResponse>
{
  public override void Configure()
  {
    Post("bikes/{id}/theft");
    Options(x => x.RequireRateLimiting(RateLimitPolicies.BikeWrite));
    Summary(s => s.Summary = "Report one of the signed-in user's bikes as stolen");
  }

  public override async Task HandleAsync(ReportTheftRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var bike = await db.Bikes.WithDetails().FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    if (!bike.CanReportStolen)
    {
      await SendAlreadyStolen(ct);
      return;
    }

    var report = bike.ReportStolen(req.StolenAt.ToUniversalTime(), req.Latitude, req.Longitude, req.LockType, clock.UtcNow);
    report.UpdateDetails(req.LocationNote, req.PoliceCaseNo);
    db.TheftReports.Add(report);
    try
    {
      await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (ex.IsUniqueViolation())
    {
      await SendAlreadyStolen(ct);
      return;
    }

    await Send.ResponseAsync(BikeResponse.From(bike), StatusCodes.Status201Created, ct);
  }

  private Task SendAlreadyStolen(CancellationToken ct)
  {
    AddError("errors.alreadyStolen");
    return Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
  }
}
