using FastEndpoints;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Bikes.List;

public class ListBikesEndpoint(AppDbContext db) : EndpointWithoutRequest<List<BikeResponse>>
{
  public override void Configure()
  {
    Get("bikes");
    Summary(s => s.Summary = "List the signed-in user's bikes");
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var userId = User.GetUserId();
    var bikes = await db.Bikes
      .AsNoTracking()
      .WithDetails()
      .Where(b => b.OwnerId == userId)
      .OrderByDescending(b => b.CreatedAt)
      .ToListAsync(ct);

    await Send.OkAsync(bikes.Select(BikeResponse.From).ToList(), ct);
  }
}
