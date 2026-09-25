namespace LeezenPass.Api.Infrastructure.Storage;

/// <summary>Blob storage for photos. Keys are relative paths like "bikes/{bikeId}/{photoId}.jpg".</summary>
public interface IFileStorage
{
  Task SaveAsync(string key, Stream content, CancellationToken ct);
  Task<Stream?> OpenReadAsync(string key, CancellationToken ct);
  Task DeleteAsync(string key, CancellationToken ct);
}
