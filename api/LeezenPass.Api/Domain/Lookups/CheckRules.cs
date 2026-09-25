namespace LeezenPass.Api.Domain.Lookups;

/// <summary>A bike whose frame number or FEIN hash matched the query exactly.</summary>
public sealed record CheckCandidate(Guid BikeId, bool IsStolen, bool HasOpenTransfer);

/// <summary>
/// "Check before you buy" (SPEC): exact match first, then the loose key.
/// A registered bike that is neither stolen nor being handed over is reported as Unknown on purpose:
/// the public check must not reveal who has registered what.
/// </summary>
public static class CheckRules
{
  /// <param name="exact">Bikes matching the frame number / FEIN hash exactly.</param>
  /// <param name="looseStolenMatch">Another stolen bike matches the loose key (O/0, I/1, S/5 ... confusions).</param>
  public static LookupResult Decide(IReadOnlyCollection<CheckCandidate> exact, bool looseStolenMatch)
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
    return looseStolenMatch ? LookupResult.PossibleMatch : LookupResult.Unknown;
  }
}
