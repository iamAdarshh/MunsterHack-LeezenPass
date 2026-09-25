using FastEndpoints;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Bikes.DeletePhoto;

public sealed record DeletePhotoRequest
{
  public Guid Id { get; init; }
  public Guid PhotoId { get; init; }
}

public class DeletePhotoEndpoint(AppDbContext db, IFileStorage storage, ILogger<DeletePhotoEndpoint> logger)
  : Endpoint<DeletePhotoRequest>
{
  public override void Configure()
  {
    Delete("bikes/{id}/photos/{photoId}");
    Summary(s => s.Summary = "Delete a photo of one of the signed-in user's bikes");
  }

  public override async Task HandleAsync(DeletePhotoRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var photo = await db.BikePhotos
      .Where(p => p.Id == req.PhotoId && p.BikeId == req.Id)
      .Where(p => db.Bikes.Any(b => b.Id == p.BikeId && b.OwnerId == userId))
      .FirstOrDefaultAsync(ct);
    if (photo is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    db.BikePhotos.Remove(photo);
    await db.SaveChangesAsync(ct);

    try
    {
      await storage.DeleteAsync(photo.Path, CancellationToken.None);
      await storage.DeleteAsync(PhotoStorageKeys.Thumbnail(photo.Path), CancellationToken.None);
    }
    catch (IOException ex)
    {
      logger.LogWarning("Could not delete photo file: {Error}", ex.GetType().Name);
    }

    await Send.NoContentAsync(ct);
  }
}
