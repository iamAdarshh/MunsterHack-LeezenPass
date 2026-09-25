namespace LeezenPass.Api.Domain.Transfers;

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

  /// <summary>Claimable: not used yet and not expired.</summary>
  public bool IsOpenAt(DateTimeOffset now) => CompletedAt is null && now < ExpiresAt;
}
