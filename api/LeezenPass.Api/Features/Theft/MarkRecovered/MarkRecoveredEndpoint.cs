using FastEndpoints;
using LeezenPass.Api.Features.Bikes;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Theft.MarkRecovered;

public sealed record MarkRecoveredRequest
{
  public Guid Id { get; init; }
}

/// <summary>Takes the bike off the public stolen list.</summary>
public class MarkRecoveredEndpoint(AppDbContext db) : Endpoint<MarkRecoveredRequest, BikeResponse>
{
  public override void Configure()
  {
    Post("bikes/{id}/recovered");
    Summary(s => s.Summary = "Mark a stolen bike as recovered");
  }

  public override async Task HandleAsync(MarkRecoveredRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var bike = await db.Bikes.WithDetails().FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    if (!bike.CanMarkRecovered)
    {
      AddError("errors.notStolen");
      await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
      return;
    }

    bike.MarkRecovered();
    await db.SaveChangesAsync(ct);

    await Send.OkAsync(BikeResponse.From(bike), ct);
  }
}
