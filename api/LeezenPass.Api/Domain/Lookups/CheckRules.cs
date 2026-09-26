using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Domain.Lookups;

/// <summary>A bike whose frame number or FEIN hash matched the query exactly.</summary>
public sealed record CheckCandidate(Guid BikeId, bool IsStolen, bool HasOpenTransfer, TrustLevel TrustLevel = TrustLevel.SelfDeclared);

/// <summary>
/// "Check before you buy" (SPEC "Check lookup order"): exact match first, then the loose key, which only searches
/// bikes the public may learn about (stolen, or with an open transfer). A registered bike that is neither stolen
/// nor being handed over is reported as Unknown on purpose: the public check must not reveal who has registered what.
/// </summary>
public static class CheckRules
{
  /// <param name="exact">Bikes matching the frame number / FEIN hash exactly.</param>
  /// <param name="looseMatch">Another stolen or open-transfer bike matches the loose key (O/0, I/1, S/5 ... confusions).</param>
  public static LookupResult Decide(IReadOnlyCollection<CheckCandidate> exact, bool looseMatch)
  {
    if (exact.Any(c => c.IsStolen))
    {
      return LookupResult.Stolen;
    }

    if (exact.Any(c => c.HasOpenTransfer))
    {
      return LookupResult.VerifiedTransfer;
    }

    // Deliberately independent of whether a clean bike matched exactly: otherwise "unknown vs possible_match"
    // would reveal which variants of a stolen frame number are registered.
    return looseMatch ? LookupResult.PossibleMatch : LookupResult.Unknown;
  }

  /// <summary>
  /// The bike whose trust level the result may show: the stolen or open-transfer exact match. Null for
  /// possible_match and unknown (a clean registered bike is never disclosed).
  /// </summary>
  public static CheckCandidate? Disclosed(IReadOnlyCollection<CheckCandidate> exact, LookupResult result) => result switch
  {
    LookupResult.Stolen => exact.First(c => c.IsStolen),
    LookupResult.VerifiedTransfer => exact.First(c => c.HasOpenTransfer),
    _ => null,
  };
}
