using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Images;
using LeezenPass.Api.Infrastructure.Storage;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Bikes.UploadPhoto;

/// <summary>Stores a photo re-encoded by ImageProcessor: upright, max 1600 px, no EXIF/GPS.</summary>
public class UploadPhotoEndpoint(AppDbContext db, IFileStorage storage, ImageProcessor images, IClock clock)
  : Endpoint<UploadPhotoRequest, BikePhotoResponse>
{
  public override void Configure()
  {
    Post("bikes/{id}/photos");
    AllowFileUploads();
    Options(x => x.RequireRateLimiting(RateLimitPolicies.Upload));
    Summary(s => s.Summary = "Upload a bike photo (multipart, header X-LeezenPass: 1)");
  }

  public override async Task HandleAsync(UploadPhotoRequest req, CancellationToken ct)
  {
    // CSRF: a cross-site form post cannot set custom headers.
    if (HttpContext.Request.Headers["X-LeezenPass"] != "1")
    {
      await Send.ForbiddenAsync(ct);
      return;
    }

    var userId = User.GetUserId();
    var bike = await db.Bikes
      .Include(b => b.Photos)
      .FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    if (bike.Photos.Count >= BikeCatalog.MaxPhotosPerBike)
    {
      AddError("errors.tooManyPhotos");
      await Send.ErrorsAsync(cancellation: ct);
      return;
    }

    ProcessedImage processed;
    try
    {
      await using var upload = req.File!.OpenReadStream();
      processed = images.Process(upload);
    }
    catch (InvalidImageException)
    {
      AddError(r => r.File, "errors.unsupportedImage");
      await Send.ErrorsAsync(cancellation: ct);
      return;
    }

    var photoId = Guid.CreateVersion7();
    var key = PhotoStorageKeys.Full(bike.Id, photoId);
    var photo = new BikePhoto(photoId, bike.Id, PhotoKinds.ByName[req.Kind], key, clock.UtcNow);

    await storage.SaveAsync(key, new MemoryStream(processed.Full), ct);
    await storage.SaveAsync(PhotoStorageKeys.Thumbnail(key), new MemoryStream(processed.Thumbnail), ct);

    db.BikePhotos.Add(photo);
    await db.SaveChangesAsync(ct);

    await Send.ResponseAsync(BikePhotoResponse.From(photo), StatusCodes.Status201Created, ct);
  }
}
