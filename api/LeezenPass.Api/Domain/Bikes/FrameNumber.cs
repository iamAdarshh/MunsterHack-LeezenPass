using System.Diagnostics.CodeAnalysis;

namespace LeezenPass.Api.Domain.Bikes;

/// <summary>
/// Frame number as entered by the user, plus the normalised key (exact lookup)
/// and the loose key (catches OCR/typing confusions like O/0 or S/5).
/// Two frame numbers are equal when their normalised keys are equal.
/// </summary>
public sealed class FrameNumber : IEquatable<FrameNumber>
{
  public const int MinLength = 4;
  public const int MaxLength = 32;
  public const int MaxRawLength = 64;

  private FrameNumber(string raw, string normalized)
  {
    Raw = raw;
    Normalized = normalized;
    Loose = ToLoose(normalized);
  }

  public string Raw { get; }
  public string Normalized { get; }
  public string Loose { get; }

  public static bool TryCreate(string? raw, [NotNullWhen(true)] out FrameNumber? frameNumber)
  {
    frameNumber = null;
    if (string.IsNullOrWhiteSpace(raw) || raw.Length > MaxRawLength)
    {
      return false;
    }

    var normalized = Normalize(raw);
    if (normalized.Length is < MinLength or > MaxLength)
    {
      return false;
    }

    frameNumber = new FrameNumber(raw.Trim(), normalized);
    return true;
  }

  public static FrameNumber Create(string raw) =>
    TryCreate(raw, out var frameNumber)
      ? frameNumber
      : throw new ArgumentException($"Frame number must contain {MinLength}-{MaxLength} letters or digits.", nameof(raw));

  /// <summary>Uppercase, ASCII letters and digits only (drops spaces, dashes, dots).</summary>
  public static string Normalize(string raw) =>
    new(raw.ToUpperInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());

  /// <summary>Maps easily confused letters to digits. Input must already be normalised.</summary>
  public static string ToLoose(string normalized) =>
    normalized.Replace('O', '0').Replace('I', '1').Replace('L', '1')
              .Replace('S', '5').Replace('B', '8').Replace('Z', '2');

  public bool Equals(FrameNumber? other) => other is not null && Normalized == other.Normalized;

  public override bool Equals(object? obj) => Equals(obj as FrameNumber);

  public override int GetHashCode() => Normalized.GetHashCode(StringComparison.Ordinal);

  public override string ToString() => Normalized;
}
