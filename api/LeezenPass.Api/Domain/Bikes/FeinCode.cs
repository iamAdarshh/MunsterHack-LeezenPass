using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace LeezenPass.Api.Domain.Bikes;

/// <summary>
/// FEIN code (police bike engraving). Personal data: it encodes the owner's address.
/// Only its HMAC-SHA256 hash is ever stored or compared. Never log or return the value.
/// </summary>
public sealed class FeinCode
{
  public const int MinLength = 6;
  public const int MaxLength = 32;
  public const int MinSecretBytes = 32;

  private readonly string _normalized;

  private FeinCode(string normalized) => _normalized = normalized;

  public static bool TryCreate(string? raw, [NotNullWhen(true)] out FeinCode? feinCode)
  {
    feinCode = null;
    if (string.IsNullOrWhiteSpace(raw) || raw.Length > MaxLength * 2)
    {
      return false;
    }

    var normalized = FrameNumber.Normalize(raw);
    if (normalized.Length is < MinLength or > MaxLength)
    {
      return false;
    }

    feinCode = new FeinCode(normalized);
    return true;
  }

  /// <summary>Lowercase hex HMAC-SHA256 of the normalised code, keyed with the server secret.</summary>
  public string ComputeHash(ReadOnlySpan<byte> secret)
  {
    if (secret.Length < MinSecretBytes)
    {
      throw new ArgumentException($"FEIN HMAC secret must be at least {MinSecretBytes} bytes.", nameof(secret));
    }

    return Convert.ToHexStringLower(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(_normalized)));
  }

  public override string ToString() => "FeinCode(***)";
}
