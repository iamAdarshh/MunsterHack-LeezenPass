using LeezenPass.Api.Domain.Goodwill;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Goodwill;

/// <summary>
/// Writes the goodwill ledger. Call it inside the transaction of the outcome it rewards, after that outcome was
/// saved, so points and outcome commit together.
/// </summary>
public class GoodwillService(AppDbContext db, IClock clock)
{
  /// <summary>Credits once per (user, action, ref); a repeat is a no-op. Returns the points credited (0 if none).</summary>
  public async Task<int> CreditAsync(Guid userId, GoodwillAction action, string refType, Guid refId, CancellationToken ct)
  {
    if (action == GoodwillAction.RegisterBike)
    {
      // Serialise count-then-insert per user (parallel registrations must not both pass the cap). Released on commit.
      await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({userId.ToString()}))", ct);
      var credited = await db.GoodwillEvents.CountAsync(
        e => e.UserId == userId && e.Action == GoodwillAction.RegisterBike && e.Status == GoodwillStatus.Credited, ct);
      if (!GoodwillPolicy.MayCreditRegistration(credited))
      {
        return 0;
      }
    }

    var entry = new GoodwillEvent(userId, action, refType, refId, clock.UtcNow);
    // ON CONFLICT DO NOTHING instead of catching the unique violation: a failed INSERT would abort the outcome's transaction.
    var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
      INSERT INTO goodwill_events (id, user_id, action, points, status, ref_type, ref_id, created_at)
      VALUES ({entry.Id}, {entry.UserId}, {entry.Action.ToString()}, {entry.Points}, {entry.Status.ToString()},
              {entry.RefType}, {entry.RefId}, {entry.CreatedAt})
      ON CONFLICT (user_id, action, ref_type, ref_id) DO NOTHING
      """, ct);
    return inserted == 1 ? entry.Points : 0;
  }

  /// <summary>A bike deleted soon after registration takes its registration points with it.</summary>
  public Task RevokeRegistrationAsync(Guid userId, Guid bikeId, CancellationToken ct)
  {
    var now = clock.UtcNow;
    return db.GoodwillEvents
      .Where(e => e.UserId == userId && e.Action == GoodwillAction.RegisterBike
        && e.RefType == GoodwillRefTypes.Bike && e.RefId == bikeId && e.Status == GoodwillStatus.Credited)
      .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, GoodwillStatus.Revoked).SetProperty(e => e.RevokedAt, now), ct);
  }
}
