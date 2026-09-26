using FastEndpoints;
using LeezenPass.Api.Domain.Goodwill;
using LeezenPass.Api.Features.Goodwill;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Storage;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Bikes.Delete;

public sealed record DeleteBikeRequest
{
  public Guid Id { get; init; }
}

/// <summary>Deletes the bike with its photos, history, theft reports and transfers (GDPR: one-click deletion).</summary>
public class DeleteBikeEndpoint(
  AppDbContext db,
  IFileStorage storage,
  IClock clock,
  GoodwillService goodwill,
  ILogger<DeleteBikeEndpoint> logger)
  : Endpoint<DeleteBikeRequest>
{
  public override void Configure()
  {
    Delete("bikes/{id}");
    Summary(s => s.Summary = "Delete one of the signed-in user's bikes");
  }

  public override async Task HandleAsync(DeleteBikeRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var bike = await db.Bikes
      .Include(b => b.Photos)
      .FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var keys = bike.Photos.SelectMany(p => new[] { p.Path, PhotoStorageKeys.Thumbnail(p.Path) }).ToList();
    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    if (GoodwillPolicy.RevokesRegistration(bike.CreatedAt, clock.UtcNow))
    {
      await goodwill.RevokeRegistrationAsync(userId, bike.Id, ct);
    }

    db.Bikes.Remove(bike);
    await db.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);

    // Files after the DB commit: a leftover file is harmless, a row pointing at a missing file is not.
    foreach (var key in keys)
    {
      try
      {
        await storage.DeleteAsync(key, CancellationToken.None);
      }
      catch (IOException ex)
      {
        logger.LogWarning("Could not delete photo file: {Error}", ex.GetType().Name);
      }
    }

    await Send.NoContentAsync(ct);
  }
}
