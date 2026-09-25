using FastEndpoints;
using FluentValidation;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Lookups;
using LeezenPass.Api.Features.Stolen;
using LeezenPass.Api.Infrastructure;
using LeezenPass.Api.Infrastructure.Captcha;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Check;

/// <summary>Exactly one of FrameNumber / FeinCode.</summary>
public sealed record CheckRequest
{
  public string? FrameNumber { get; init; }
  public string? FeinCode { get; init; }
  public string? CaptchaToken { get; init; }
}

/// <summary>Bikes only for stolen / possible_match (public data, district only). Otherwise just the status.</summary>
public sealed record CheckResponse(LookupResult Result, IReadOnlyList<StolenBikeResponse> Bikes);

public class CheckValidator : Validator<CheckRequest>
{
  public CheckValidator()
  {
    RuleFor(x => x)
      .Must(x => string.IsNullOrWhiteSpace(x.FrameNumber) != string.IsNullOrWhiteSpace(x.FeinCode))
      .WithName("frameNumber").WithMessage("errors.checkInput");
    RuleFor(x => x.FrameNumber)
      .Must(f => Domain.Bikes.FrameNumber.TryCreate(f, out _)).WithMessage("errors.frameNumberInvalid")
      .When(x => !string.IsNullOrWhiteSpace(x.FrameNumber));
    RuleFor(x => x.FeinCode)
      .Must(f => Domain.Bikes.FeinCode.TryCreate(f, out _)).WithMessage("errors.feinCodeInvalid")
      .When(x => !string.IsNullOrWhiteSpace(x.FeinCode));
  }
}

/// <summary>
/// Public "check before you buy". Rate-limited per IP, captcha, every lookup logged as keyed hashes only.
/// </summary>
public class CheckEndpoint(
  AppDbContext db,
  ICaptchaVerifier captcha,
  IClock clock,
  IOptions<FeinOptions> fein) : Endpoint<CheckRequest, CheckResponse>
{
  public override void Configure()
  {
    Post("check");
    AllowAnonymous();
    Options(x => x.RequireRateLimiting(RateLimitPolicies.Check));
    Summary(s => s.Summary = "Check a frame number or FEIN code before buying (public)");
  }

  public override async Task HandleAsync(CheckRequest req, CancellationToken ct)
  {
    if (!await captcha.VerifyAsync(req.CaptchaToken, ct))
    {
      AddError("errors.captchaFailed");
      await Send.ErrorsAsync(cancellation: ct);
      return;
    }

    var now = clock.UtcNow;
    var secret = fein.Value.SecretBytes();
    var bikes = db.Bikes.AsNoTracking();

    string queryHash;
    IQueryable<Bike> exactQuery;
    string? looseKey = null;

    if (Domain.Bikes.FrameNumber.TryCreate(req.FrameNumber, out var frameNumber))
    {
      queryHash = LookupHashing.QueryHash("frame", frameNumber.Normalized, secret);
      exactQuery = bikes.Where(b => b.FrameNoNorm == frameNumber.Normalized);
      looseKey = frameNumber.Loose;
    }
    else
    {
      Domain.Bikes.FeinCode.TryCreate(req.FeinCode, out var feinCode);
      var feinHash = feinCode!.ComputeHash(secret);
      queryHash = LookupHashing.QueryHash("fein", feinHash, secret);
      exactQuery = bikes.Where(b => b.FeinCodeHash == feinHash);
    }

    var exact = await exactQuery
      .Select(b => new CheckCandidate(
        b.Id,
        b.Status == BikeStatus.Stolen,
        db.OwnershipTransfers.Any(t => t.BikeId == b.Id && t.CompletedAt == null && t.ExpiresAt > now)))
      .ToListAsync(ct);

    // Loose key only against stolen bikes, and always (not only when nothing matched exactly).
    var exactIds = exact.Select(c => c.BikeId).ToList();
    var looseStolenIds = looseKey is not null
      ? await bikes
        .Where(b => b.FrameNoLoose == looseKey && b.Status == BikeStatus.Stolen && !exactIds.Contains(b.Id))
        .Select(b => b.Id)
        .ToListAsync(ct)
      : [];

    var result = CheckRules.Decide(exact, looseStolenIds.Count > 0);

    IReadOnlyCollection<Guid> shownIds = result switch
    {
      LookupResult.Stolen => exact.Where(c => c.IsStolen).Select(c => c.BikeId).ToList(),
      LookupResult.PossibleMatch => looseStolenIds,
      _ => [],
    };
    var shown = shownIds.Count == 0
      ? []
      : (await db.StolenBikes(new StolenBikeFilter(BikeIds: shownIds)).ToListAsync(ct)).Select(r => r.ToResponse()).ToList();

    var client = ClientKey.For(HttpContext.Connection.RemoteIpAddress);
    db.Lookups.Add(new Lookup(LookupHashing.IpHash(client, DateOnly.FromDateTime(now.UtcDateTime), secret), queryHash, result, now));
    await db.SaveChangesAsync(ct);

    HttpContext.Response.Headers.CacheControl = "no-store";
    await Send.OkAsync(new CheckResponse(result, shown), ct);
  }
}
