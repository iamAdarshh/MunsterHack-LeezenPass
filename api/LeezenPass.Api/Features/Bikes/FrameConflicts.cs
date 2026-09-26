using System.Collections.Concurrent;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Lookups;
using LeezenPass.Api.Infrastructure;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Email;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Bikes;

/// <summary>The bike that already has a frame number. Never sent to the client.</summary>
public sealed record ExistingBike(Guid Id, bool IsStolen, string? Brand, string? Model, string? OwnerEmail);

/// <summary>
/// "Frame number already registered" handling for register and update (SPEC feature 5): the same answer whether the
/// existing bike is stolen or not, max 5 conflicts per user and day (in memory; resets on restart), a security event
/// in the log (hashed IP) and an email to the owner of a stolen bike, sent in the background so both cases take
/// the same time.
/// </summary>
public class FrameConflicts(
  IServiceScopeFactory scopes,
  IClock clock,
  IOptions<FeinOptions> fein,
  ILogger<FrameConflicts> logger)
{
  public const int MaxPerUserPerDay = 5;

  private readonly ConcurrentDictionary<(Guid UserId, DateOnly Day), int> _counts = new();

  private DateOnly Today => DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

  /// <summary>Checked before the lookup, for every write, so hitting the cap reveals nothing.</summary>
  public bool LimitReached(Guid userId) =>
    _counts.TryGetValue((userId, Today), out var count) && count >= MaxPerUserPerDay;

  /// <summary>Same query whether the existing bike is stolen or not.</summary>
  public static Task<ExistingBike?> FindAsync(AppDbContext db, string frameNoNorm, CancellationToken ct) =>
    db.Bikes
      .Where(b => b.FrameNoNorm == frameNoNorm)
      .Select(b => new ExistingBike(
        b.Id,
        b.Status == BikeStatus.Stolen,
        b.Brand,
        b.Model,
        db.Users.Where(u => u.Id == b.OwnerId).Select(u => u.Email).FirstOrDefault()))
      .FirstOrDefaultAsync(ct);

  /// <param name="existing">Null when the conflict came from the unique index (registered in the same moment).</param>
  /// <param name="userId">Who tried to use the frame number.</param>
  /// <param name="client">Client key (IP) for the hashed security log entry.</param>
  public void Record(ExistingBike? existing, Guid userId, string client)
  {
    var today = Today;
    _counts.AddOrUpdate((userId, today), 1, (_, count) => count + 1);
    foreach (var key in _counts.Keys.Where(k => k.Day < today))
    {
      _counts.TryRemove(key, out _);
    }

    // Security event (log only for now): hashed IP, no frame number, no emails.
    var ipHash = LookupHashing.IpHash(client, today, fein.Value.SecretBytes());
    logger.LogWarning(
      "Security event {Kind}: user {UserId}, existing bike {BikeId}, ip {IpHash}",
      existing?.IsStolen == true ? "stolen_frame_registration_attempt" : "duplicate_frame_registration",
      userId, existing?.Id, ipHash[..16]);

    if (existing is { IsStolen: true, OwnerEmail: { } ownerEmail })
    {
      // Fire and forget: waiting for SMTP would make the stolen case measurably slower than the other one.
      _ = NotifyOwnerAsync(ownerEmail, existing);
    }
  }

  private async Task NotifyOwnerAsync(string ownerEmail, ExistingBike bike)
  {
    await Task.Yield();
    try
    {
      await using var scope = scopes.CreateAsyncScope();
      var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();
      var name = string.Join(' ', new[] { bike.Brand, bike.Model }.Where(s => !string.IsNullOrWhiteSpace(s)));
      await email.SendAsync(new EmailMessage(
        ownerEmail,
        "LeezenPass: Jemand wollte dein gestohlenes Rad registrieren",
        $"""
        Hallo,

        gerade hat jemand versucht, dein als gestohlen gemeldetes Rad{(name.Length > 0 ? $" ({name})" : "")} bei LeezenPass
        mit derselben Rahmennummer zu registrieren. Die Registrierung wurde abgelehnt.

        Das kann ein Hinweis auf dein Rad sein. Gib ihn bitte mit deinem Aktenzeichen an die Polizei weiter
        (Notruf 110, Internetwache NRW: https://polizei.nrw/internetwache). Bitte konfrontiere niemanden selbst.

        Dein LeezenPass-Team
        """), CancellationToken.None);
    }
    catch (Exception ex)
    {
      logger.LogWarning("Stolen-bike owner notification failed ({Error})", ex.GetType().Name);
    }
  }
}
