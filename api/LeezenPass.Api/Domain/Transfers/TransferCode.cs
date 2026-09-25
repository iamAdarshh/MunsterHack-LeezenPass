using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace LeezenPass.Api.Domain.Transfers;

/// <summary>
/// One-time code a seller gives a buyer to hand over a bike.
/// 8 characters from an alphabet without look-alikes (no I, L, O, 0, 1), stored as SHA-256 hash only.
/// </summary>
public sealed class TransferCode
{
  public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
  public const int Length = 8;
  public static readonly TimeSpan Validity = TimeSpan.FromHours(48);

  private TransferCode(string value) => Value = value;

  /// <summary>The plain code. Show it to the seller once; never store or log it.</summary>
  public string Value { get; }

  public static TransferCode Generate() => new(RandomNumberGenerator.GetString(Alphabet, Length));

  /// <summary>Accepts user input like "abcd-efgh" or "ABCD EFGH".</summary>
  public static bool TryParse(string? input, [NotNullWhen(true)] out TransferCode? code)
  {
    code = null;
    if (string.IsNullOrWhiteSpace(input))
    {
      return false;
    }

    var cleaned = new string(input.ToUpperInvariant().Where(c => c is not (' ' or '-')).ToArray());
    if (cleaned.Length != Length || !cleaned.All(Alphabet.Contains))
    {
      return false;
    }

    code = new TransferCode(cleaned);
    return true;
  }

  public string ComputeHash() => Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(Value)));

  public override string ToString() => "TransferCode(***)";
}
