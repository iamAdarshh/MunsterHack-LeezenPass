using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Domain.Verification;

/// <summary>What the vision model read on a receipt.</summary>
public sealed record ReceiptReading(string? FrameNumberCandidate, string? Brand, string? Model, DateOnly? PurchaseDate, double Confidence);

/// <summary>What the vision model read on a possession photo (frame number with the challenge code next to it).</summary>
public sealed record PossessionReading(bool CodeVisible, string? CodeValue, string? FrameNumberCandidate, double Confidence);

public enum CheckOutcome { Passed, Failed, Retry }

/// <summary>Receipt + possession check rules (SPEC feature 5, "Receipt + possession AI calls").</summary>
public static class VerificationRules
{
  /// <summary>Below this the photo is unclear: ask for a new photo instead of failing.</summary>
  public const double MinConfidence = 0.6;

  /// <summary>Pass if the frame number loose-matches, or brand matches AND model fuzzy-matches AND the date is not in the future.</summary>
  public static CheckOutcome Receipt(ReceiptReading reading, Bike bike, DateOnly today)
  {
    if (reading.Confidence < MinConfidence)
    {
      return CheckOutcome.Retry;
    }

    if (FrameMatches(reading.FrameNumberCandidate, bike))
    {
      return CheckOutcome.Passed;
    }

    var brand = Norm(reading.Brand);
    var brandMatches = brand.Length > 0 && brand == Norm(bike.Brand);
    var dateOk = reading.PurchaseDate is { } date && date <= today;
    return brandMatches && dateOk && ModelMatches(reading.Model, bike.Model) ? CheckOutcome.Passed : CheckOutcome.Failed;
  }

  /// <summary>Pass if the code on the photo equals the challenge and the frame number loose-matches.</summary>
  public static CheckOutcome Possession(PossessionReading reading, string expectedCode, Bike bike)
  {
    if (reading.Confidence < MinConfidence || !reading.CodeVisible)
    {
      return CheckOutcome.Retry;
    }

    var code = new string((reading.CodeValue ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
    return code == expectedCode && FrameMatches(reading.FrameNumberCandidate, bike) ? CheckOutcome.Passed : CheckOutcome.Failed;
  }

  /// <summary>Evidence-checked needs both checks passed by the current owner.</summary>
  public static bool BothPassed(IEnumerable<EvidenceKind> passed)
  {
    var kinds = passed.ToHashSet();
    return kinds.Contains(EvidenceKind.Receipt) && kinds.Contains(EvidenceKind.Possession);
  }

  private static bool FrameMatches(string? candidate, Bike bike) =>
    FrameNumber.TryCreate(candidate, out var read) && read.Loose == bike.FrameNoLoose;

  /// <summary>Case/space-insensitive; equal, one contains the other (3+ chars), or a small typo (1 edit from 3 chars, 2 from 5).</summary>
  public static bool ModelMatches(string? read, string? registered)
  {
    var (a, b) = (Norm(read), Norm(registered));
    if (a.Length == 0 || b.Length == 0)
    {
      return false;
    }

    // Short names match too easily ("e" is in everything): containment needs 3+ characters, typos 5+.
    var shorter = Math.Min(a.Length, b.Length);
    var contains = shorter >= 3 && (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal));
    return a == b || contains || Distance(a, b) <= (shorter >= 5 ? 2 : shorter >= 3 ? 1 : 0);
  }

  private static string Norm(string? value) =>
    new((value ?? string.Empty).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

  private static int Distance(string a, string b)
  {
    var previous = Enumerable.Range(0, b.Length + 1).ToArray();
    for (var i = 1; i <= a.Length; i++)
    {
      var current = new int[b.Length + 1];
      current[0] = i;
      for (var j = 1; j <= b.Length; j++)
      {
        current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
      }

      previous = current;
    }

    return previous[b.Length];
  }
}
