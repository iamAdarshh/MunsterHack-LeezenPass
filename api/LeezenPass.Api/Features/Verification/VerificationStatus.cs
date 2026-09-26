using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Goodwill;
using LeezenPass.Api.Domain.Verification;
using LeezenPass.Api.Features.Goodwill;
using LeezenPass.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Verification;

/// <summary>Owner view of the ownership checks for the current owner (a previous owner's checks don't count).</summary>
public sealed record VerificationStatusResponse(
  TrustLevel TrustLevel,
  EvidenceStatus? Receipt,
  EvidenceStatus? Possession,
  DateTimeOffset? ChallengeExpiresAt);

/// <summary>Result of one receipt or possession photo. <c>retry</c>: photo unclear, take a new one (not a failure).</summary>
/// <param name="Outcome">passed | failed | retry.</param>
/// <param name="Status">Status after this check.</param>
/// <param name="PointsCredited">Goodwill points this check earned (only when the bike just reached EvidenceChecked).</param>
public sealed record VerificationResultResponse(CheckOutcome Outcome, VerificationStatusResponse Status, int PointsCredited = 0);

public static class VerificationQueries
{
  /// <summary>Latest result per kind: passed wins over failed.</summary>
  public static async Task<VerificationStatusResponse> StatusAsync(
    this AppDbContext db, Bike bike, Guid userId, DateTimeOffset now, CancellationToken ct)
  {
    var evidence = await db.OwnershipEvidence
      .Where(e => e.BikeId == bike.Id && e.UserId == userId)
      .Select(e => new { e.Kind, e.Status })
      .ToListAsync(ct);
    var challengeExpiresAt = await db.PossessionChallenges
      .Where(c => c.BikeId == bike.Id && c.UserId == userId && c.UsedAt == null && c.ExpiresAt > now)
      .MaxAsync(c => (DateTimeOffset?)c.ExpiresAt, ct);

    EvidenceStatus? Of(EvidenceKind kind)
    {
      var statuses = evidence.Where(e => e.Kind == kind).Select(e => e.Status).ToList();
      return statuses.Count == 0 ? null : statuses.Contains(EvidenceStatus.Passed) ? EvidenceStatus.Passed : EvidenceStatus.Failed;
    }

    return new VerificationStatusResponse(bike.TrustLevel, Of(EvidenceKind.Receipt), Of(EvidenceKind.Possession), challengeExpiresAt);
  }

  /// <summary>
  /// Records one check; raises the trust level once receipt AND possession have passed for this owner.
  /// Returns true when this check raised it (the caller credits goodwill after saving).
  /// </summary>
  public static async Task<bool> RecordAsync(
    this AppDbContext db, Bike bike, Guid userId, OwnershipEvidence evidence, DateTimeOffset now, CancellationToken ct)
  {
    db.OwnershipEvidence.Add(evidence);
    if (evidence.Status != EvidenceStatus.Passed)
    {
      return false;
    }

    var passed = await db.OwnershipEvidence
      .Where(e => e.BikeId == bike.Id && e.UserId == userId && e.Status == EvidenceStatus.Passed)
      .Select(e => e.Kind)
      .ToListAsync(ct);
    var before = bike.TrustLevel;
    if (VerificationRules.BothPassed(passed.Append(evidence.Kind)))
    {
      bike.MarkEvidenceChecked(now);
    }

    return bike.TrustLevel != before;
  }

  /// <summary>Saves the check and, if it raised the trust level, credits goodwill in the same transaction.</summary>
  public static async Task<int> SaveCheckAsync(
    this AppDbContext db, GoodwillService goodwill, Bike bike, Guid userId, bool trustRaised, CancellationToken ct)
  {
    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    await db.SaveChangesAsync(ct);
    var points = trustRaised
      ? await goodwill.CreditAsync(userId, GoodwillAction.EvidenceVerified, GoodwillRefTypes.Frame, GoodwillRefTypes.FrameId(bike.FrameNoNorm), ct)
      : 0;
    await transaction.CommitAsync(ct);
    return points;
  }
}
