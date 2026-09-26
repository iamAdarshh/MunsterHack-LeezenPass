using System.Security.Cryptography;
using System.Text;

namespace LeezenPass.Api.Domain.Verification;

/// <summary>
/// Possession check (SPEC feature 5): a random 4-digit code the owner photographs next to the frame number.
/// Valid 10 minutes, single use, bound to bike and user, stored as a hash only.
/// </summary>
public class PossessionChallenge
{
  public static readonly TimeSpan Validity = TimeSpan.FromMinutes(10);

  private PossessionChallenge() { }

  private PossessionChallenge(Guid bikeId, Guid userId, string codeHash, DateTimeOffset now)
  {
    Id = Guid.CreateVersion7();
    BikeId = bikeId;
    UserId = userId;
    CodeHash = codeHash;
    CreatedAt = now;
    ExpiresAt = now + Validity;
  }

  public Guid Id { get; private set; }
  public Guid BikeId { get; private set; }
  public Guid UserId { get; private set; }
  public string CodeHash { get; private set; } = string.Empty;
  public DateTimeOffset CreatedAt { get; private set; }
  public DateTimeOffset ExpiresAt { get; private set; }
  public DateTimeOffset? UsedAt { get; private set; }

  /// <summary>Returns the plain code once, for the owner's screen. Only its hash is stored.</summary>
  public static (PossessionChallenge Challenge, string Code) Create(Guid bikeId, Guid userId, DateTimeOffset now)
  {
    var code = RandomNumberGenerator.GetInt32(0, 10_000).ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
    return (new PossessionChallenge(bikeId, userId, Hash(code, bikeId, userId), now), code);
  }

  public static string Hash(string code, Guid bikeId, Guid userId) =>
    Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes($"{code.Trim()}:{bikeId:N}:{userId:N}")));

  public bool IsOpenAt(DateTimeOffset now) => UsedAt is null && now < ExpiresAt;

  public bool Matches(string code) => CryptographicOperations.FixedTimeEquals(
    Encoding.ASCII.GetBytes(Hash(code, BikeId, UserId)), Encoding.ASCII.GetBytes(CodeHash));

  public void MarkUsed(DateTimeOffset now)
  {
    if (!IsOpenAt(now))
    {
      throw new InvalidOperationException("Challenge is expired or already used.");
    }

    UsedAt = now;
  }
}
