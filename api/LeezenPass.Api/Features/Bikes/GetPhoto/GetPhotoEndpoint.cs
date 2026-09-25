using FastEndpoints;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Bikes.GetPhoto;

public sealed record GetPhotoRequest
{
  public Guid Id { get; init; }
  public Guid PhotoId { get; init; }

  /// <summary>"thumb" for the 400 px version.</summary>
  [QueryParam]
  public string? Size { get; init; }
}

public class GetPhotoEndpoint(AppDbContext db, IFileStorage storage) : Endpoint<GetPhotoRequest>
{
  public override void Configure()
  {
    Get("bikes/{id}/photos/{photoId}");
    Summary(s => s.Summary = "Photo of one of the signed-in user's bikes (JPEG)");
  }

  public override async Task HandleAsync(GetPhotoRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var path = await db.BikePhotos
      .Where(p => p.Id == req.PhotoId && p.BikeId == req.Id)
      .Where(p => db.Bikes.Any(b => b.Id == p.BikeId && b.OwnerId == userId))
      .Select(p => p.Path)
      .FirstOrDefaultAsync(ct);

    var key = path is null ? null : req.Size == "thumb" ? PhotoStorageKeys.Thumbnail(path) : path;
    var stream = key is null ? null : await storage.OpenReadAsync(key, ct);
    if (stream is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    // Photo ids are never reused, so the bytes behind a URL never change.
    HttpContext.Response.Headers.CacheControl = "private, max-age=86400, immutable";
    await Send.StreamAsync(stream, contentType: "image/jpeg", cancellation: ct);
  }
}
