namespace LeezenPass.Api.Domain.Goodwill;

/// <summary>What earns goodwill points. Only verified outcomes, never raw submissions (SPEC feature 6).</summary>
public enum GoodwillAction
{
  RegisterBike,
  EvidenceVerified,
  PartnerRegistration,
}

public enum GoodwillStatus { Credited, Revoked }

public sealed record Badge(string Key, int Threshold);

/// <summary>The only place with point values, caps and badge thresholds.</summary>
public static class GoodwillPolicy
{
  /// <summary>Only the first bikes of a user earn registration points (no farming by registering junk).</summary>
  public const int MaxRegisterBikeCredits = 3;

  /// <summary>Deleting a bike within this time takes its registration points back.</summary>
  public static readonly TimeSpan RevokeRegisterWithin = TimeSpan.FromHours(24);

  public static readonly IReadOnlyList<Badge> Badges =
  [
    new("starter", 10),
    new("protector", 50),
    new("hero", 200),
  ];

  public static int Points(GoodwillAction action) => action switch
  {
    GoodwillAction.RegisterBike => 10,
    GoodwillAction.EvidenceVerified => 20,
    GoodwillAction.PartnerRegistration => 10,
    _ => throw new ArgumentOutOfRangeException(nameof(action)),
  };

  /// <summary>Highest badge reached, or null below the first threshold.</summary>
  public static Badge? BadgeFor(int total) => Badges.LastOrDefault(b => total >= b.Threshold);

  public static Badge? NextBadge(int total) => Badges.FirstOrDefault(b => total < b.Threshold);

  public static bool MayCreditRegistration(int creditedRegistrations) => creditedRegistrations < MaxRegisterBikeCredits;

  public static bool RevokesRegistration(DateTimeOffset registeredAt, DateTimeOffset deletedAt) =>
    deletedAt - registeredAt < RevokeRegisterWithin;
}
