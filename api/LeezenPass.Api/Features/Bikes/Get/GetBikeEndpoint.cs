using FastEndpoints;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Bikes.Get;

public sealed record GetBikeRequest
{
  public Guid Id { get; init; }
}

public class GetBikeEndpoint(AppDbContext db) : Endpoint<GetBikeRequest, BikeResponse>
{
  public override void Configure()
  {
    Get("bikes/{id}");
    Summary(s => s.Summary = "Get one of the signed-in user's bikes");
  }

  public override async Task HandleAsync(GetBikeRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    // 404 (not 403) for other people's bikes, so ids don't leak existence.
    var bike = await db.Bikes
      .AsNoTracking()
      .WithDetails()
      .FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);

    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    await Send.OkAsync(BikeResponse.From(bike), ct);
  }
}
