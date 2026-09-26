using FastEndpoints;
using LeezenPass.Api.Domain.Goodwill;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Goodwill.Mine;

public sealed record BadgeResponse(string Key, int Threshold);

public sealed record GoodwillEntryResponse(GoodwillAction Action, int Points, GoodwillStatus Status, DateTimeOffset CreatedAt);

/// <param name="Total">Credited points (revoked ones don't count).</param>
/// <param name="Badge">Highest badge reached, null below the first threshold.</param>
/// <param name="NextBadge">Next badge to reach, null at the top.</param>
/// <param name="History">Newest first.</param>
public sealed record GoodwillResponse(int Total, BadgeResponse? Badge, BadgeResponse? NextBadge, IReadOnlyList<GoodwillEntryResponse> History);

/// <summary>The signed-in user's goodwill points, badge and history. Only ever the caller's own ledger.</summary>
public class GetMyGoodwillEndpoint(AppDbContext db) : EndpointWithoutRequest<GoodwillResponse>
{
  public override void Configure()
  {
    Get("me/goodwill");
    Summary(s => s.Summary = "My goodwill points, badge and history");
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var userId = User.GetUserId();
    var history = await db.GoodwillEvents
      .Where(e => e.UserId == userId)
      .OrderByDescending(e => e.CreatedAt)
      .Select(e => new GoodwillEntryResponse(e.Action, e.Points, e.Status, e.CreatedAt))
      .ToListAsync(ct);

    var total = history.Where(e => e.Status == GoodwillStatus.Credited).Sum(e => e.Points);
    static BadgeResponse? ToResponse(Badge? badge) => badge is null ? null : new BadgeResponse(badge.Key, badge.Threshold);

    await Send.OkAsync(new GoodwillResponse(total, ToResponse(GoodwillPolicy.BadgeFor(total)), ToResponse(GoodwillPolicy.NextBadge(total)), history), ct);
  }
}
