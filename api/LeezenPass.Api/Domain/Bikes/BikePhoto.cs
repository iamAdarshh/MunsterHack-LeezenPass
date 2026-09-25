namespace LeezenPass.Api.Domain.Bikes;

public class BikePhoto
{
  private BikePhoto() { }

  public BikePhoto(Guid bikeId, PhotoKind kind, string path, DateTimeOffset createdAt)
  {
    Id = Guid.CreateVersion7();
    BikeId = bikeId;
    Kind = kind;
    Path = path;
    CreatedAt = createdAt;
  }

  public Guid Id { get; private set; }
  public Guid BikeId { get; private set; }
  public PhotoKind Kind { get; private set; }

  /// <summary>Storage key (see IFileStorage), not a URL.</summary>
  public string Path { get; private set; } = string.Empty;

  public DateTimeOffset CreatedAt { get; private set; }
}
