using System.Buffers.Text;
using System.Security.Cryptography;

namespace LeezenPass.Api.Domain.Transfers;

/// <summary>
/// Seller hands a bike to a buyer with a one-time code (stored hashed, 48 h, single use).
/// On completion a verify token is issued for the certificate's QR code.
/// </summary>
public class OwnershipTransfer
{
  private OwnershipTransfer() { }

  public OwnershipTransfer(Guid bikeId, Guid fromUserId, TransferCode code, DateTimeOffset now)
  {
    Id = Guid.CreateVersion7();
    BikeId = bikeId;
    FromUserId = fromUserId;
    CodeHash = code.ComputeHash();
    CreatedAt = now;
    ExpiresAt = now + TransferCode.Validity;
  }

  public Guid Id { get; private set; }
  public Guid BikeId { get; private set; }
  public Guid FromUserId { get; private set; }
  public Guid? ToUserId { get; private set; }
  public string CodeHash { get; private set; } = string.Empty;
  public DateTimeOffset CreatedAt { get; private set; }
  public DateTimeOffset ExpiresAt { get; private set; }
  public DateTimeOffset? CompletedAt { get; private set; }

  /// <summary>Unguessable id behind /verify/{token}; set when the transfer completes.</summary>
  public string? VerifyToken { get; private set; }

  /// <summary>Claimable: not used yet and not expired.</summary>
  public bool IsOpenAt(DateTimeOffset now) => CompletedAt is null && now < ExpiresAt;

  /// <summary>Buyer claims the code. Single use: throws when expired, used or claimed by the seller.</summary>
  public void Complete(Guid toUserId, DateTimeOffset now)
  {
    if (!IsOpenAt(now))
    {
      throw new InvalidOperationException("Transfer code is expired or already used.");
    }

    if (toUserId == FromUserId)
    {
      throw new InvalidOperationException("The seller cannot claim their own transfer.");
    }

    ToUserId = toUserId;
    CompletedAt = now;
    VerifyToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));
  }

  /// <summary>Seller withdraws the code (or a new code replaces it).</summary>
  public void Cancel(DateTimeOffset now)
  {
    if (IsOpenAt(now))
    {
      ExpiresAt = now;
    }
  }
}
