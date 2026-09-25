using System.Security.Cryptography;
using System.Text;

namespace LeezenPass.Api.Domain.Lookups;

/// <summary>
/// Keyed hashes for the lookup log: abuse can be spotted (same IP, same query) without storing IPs,
/// frame numbers or FEIN codes. Keyed with the server secret, so short inputs can't be brute-forced from the log.
/// </summary>
public static class LookupHashing
{
  /// <summary>Changes every day (daily salt), so IPs can't be tracked over time.</summary>
  public static string IpHash(string ip, DateOnly day, ReadOnlySpan<byte> secret) =>
    Hmac(secret, $"ip:{day:yyyy-MM-dd}:{ip}");

  /// <param name="kind">"frame" or "fein".</param>
  /// <param name="value">Normalised frame number, or the FEIN hash (never the FEIN code).</param>
  /// <param name="secret">Server secret.</param>
  public static string QueryHash(string kind, string value, ReadOnlySpan<byte> secret) =>
    Hmac(secret, $"{kind}:{value}");

  private static string Hmac(ReadOnlySpan<byte> secret, string value) =>
    Convert.ToHexStringLower(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(value)));
}
