namespace LeezenPass.Api.Domain.Bikes;

public class BikeOwnershipHistory
{
  private BikeOwnershipHistory() { }

  public BikeOwnershipHistory(Guid bikeId, Guid ownerId, DateTimeOffset fromAt)
  {
    Id = Guid.CreateVersion7();
    BikeId = bikeId;
    OwnerId = ownerId;
    FromAt = fromAt;
  }

  public Guid Id { get; private set; }
  public Guid BikeId { get; private set; }
  public Guid OwnerId { get; private set; }
  public DateTimeOffset FromAt { get; private set; }
  public DateTimeOffset? ToAt { get; private set; }
}
