using FastEndpoints;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Stolen.GetPhoto;

public sealed record GetStolenPhotoRequest
{
  public string Token { get; init; } = string.Empty;
  public Guid PhotoId { get; init; }

  [QueryParam]
  public string? Size { get; init; }
}

/// <summary>Public photo of a stolen bike. Only side/detail photos, only while the bike is stolen.</summary>
public class GetStolenPhotoEndpoint(AppDbContext db, IFileStorage storage) : Endpoint<GetStolenPhotoRequest>
{
  public override void Configure()
  {
    Get("stolen/{token}/photos/{photoId}");
    AllowAnonymous();
    Summary(s => s.Summary = "Public photo (JPEG) of a stolen bike");
  }

  public override async Task HandleAsync(GetStolenPhotoRequest req, CancellationToken ct)
  {
    var path = await db.BikePhotos
      .Where(p => p.Id == req.PhotoId && StolenBikeQueries.PublicPhotoKinds.Contains(p.Kind))
      .Where(p => db.Bikes.Any(b => b.Id == p.BikeId && b.PublicToken == req.Token && b.Status == BikeStatus.Stolen))
      .Select(p => p.Path)
      .FirstOrDefaultAsync(ct);

    var key = path is null ? null : req.Size == "thumb" ? PhotoStorageKeys.Thumbnail(path) : path;
    var stream = key is null ? null : await storage.OpenReadAsync(key, ct);
    if (stream is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    // Browser-only, short: once the bike is recovered the photo must disappear (no shared/proxy caches).
    HttpContext.Response.Headers.CacheControl = "private, max-age=300";
    await Send.StreamAsync(stream, contentType: "image/jpeg", cancellation: ct);
  }
}
