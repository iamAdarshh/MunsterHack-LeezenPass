using LeezenPass.Api.Domain.Goodwill;

namespace LeezenPass.Api.Tests.Domain;

public class GoodwillTests
{
  private static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

  [Theory]
  [InlineData(GoodwillAction.RegisterBike, 10)]
  [InlineData(GoodwillAction.EvidenceVerified, 20)]
  [InlineData(GoodwillAction.PartnerRegistration, 10)]
  public void Points_come_from_the_policy(GoodwillAction action, int points)
  {
    Assert.Equal(points, GoodwillPolicy.Points(action));
    Assert.Equal(points, new GoodwillEvent(Guid.NewGuid(), action, GoodwillRefTypes.Bike, Guid.NewGuid(), Now).Points);
  }

  [Theory]
  [InlineData(0, null, "starter")]
  [InlineData(9, null, "starter")]
  [InlineData(10, "starter", "protector")]
  [InlineData(70, "protector", "hero")]
  [InlineData(200, "hero", null)]
  public void Badges_follow_the_thresholds(int total, string? badge, string? next)
  {
    Assert.Equal(badge, GoodwillPolicy.BadgeFor(total)?.Key);
    Assert.Equal(next, GoodwillPolicy.NextBadge(total)?.Key);
  }

  [Fact]
  public void Only_the_first_three_bikes_earn_registration_points()
  {
    Assert.True(GoodwillPolicy.MayCreditRegistration(2));
    Assert.False(GoodwillPolicy.MayCreditRegistration(3));
  }

  [Fact]
  public void Deleting_within_24_hours_revokes_registration_points()
  {
    Assert.True(GoodwillPolicy.RevokesRegistration(Now, Now.AddHours(23)));
    Assert.False(GoodwillPolicy.RevokesRegistration(Now, Now.AddHours(25)));
  }

  [Fact]
  public void A_new_ledger_entry_is_credited()
  {
    var entry = new GoodwillEvent(Guid.NewGuid(), GoodwillAction.RegisterBike, GoodwillRefTypes.Bike, Guid.NewGuid(), Now);

    Assert.Equal(GoodwillStatus.Credited, entry.Status);
    Assert.Null(entry.RevokedAt);
  }

  [Fact]
  public void Frame_ref_is_stable_per_frame_number()
  {
    Assert.Equal(GoodwillRefTypes.FrameId("ST88301145"), GoodwillRefTypes.FrameId("ST88301145"));
    Assert.NotEqual(GoodwillRefTypes.FrameId("ST88301145"), GoodwillRefTypes.FrameId("ST88301146"));
  }
}
