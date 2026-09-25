namespace LeezenPass.Api.Infrastructure.Storage;

/// <summary>Where bike photos live in IFileStorage. BikePhoto.Path holds the full-size key.</summary>
public static class PhotoStorageKeys
{
  public static string Full(Guid bikeId, Guid photoId) => $"bikes/{bikeId:N}/{photoId:N}.jpg";

  public static string Thumbnail(string fullKey) => fullKey[..^".jpg".Length] + "_thumb.jpg";
}
