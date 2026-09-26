using System.Text.Json;
using FastEndpoints;
using FluentValidation;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Verification;
using LeezenPass.Api.Features.Bikes.UploadPhoto;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Images;
using LeezenPass.Api.Infrastructure.Time;
using LeezenPass.Api.Infrastructure.Vision;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Verification.Possession;

public sealed record VerifyPossessionRequest
{
  public Guid Id { get; init; }
  public IFormFile? File { get; init; }

  /// <summary>The code the app is showing; must match an open challenge for this bike and user.</summary>
  public string Code { get; init; } = string.Empty;
}

public class VerifyPossessionValidator : Validator<VerifyPossessionRequest>
{
  public VerifyPossessionValidator()
  {
    RuleFor(x => x.Code).Matches("^[0-9]{4}$").WithMessage("errors.challengeInvalid");
    RuleFor(x => x.File).NotNull().WithMessage("errors.required");
    RuleFor(x => x.File!.Length)
      .InclusiveBetween(1, UploadPhotoValidator.MaxBytes).WithMessage("errors.fileTooLarge")
      .When(x => x.File is not null);
    RuleFor(x => x.File!.ContentType)
      .Must(UploadPhotoValidator.AllowedTypes.Contains).WithMessage("errors.unsupportedImage")
      .When(x => x.File is not null);
  }
}

/// <summary>
/// Possession check: photo of the frame number with the challenge code next to it. The photo is only read,
/// never stored. The challenge is used up by a pass or a fail (not by an unclear photo); wrong or expired codes get 400.
/// </summary>
public class VerifyPossessionEndpoint(
  AppDbContext db,
  IVisionExtractor vision,
  ImageProcessor images,
  IClock clock,
  IOptions<VisionOptions> visionOptions,
  ILogger<VerifyPossessionEndpoint> logger) : Endpoint<VerifyPossessionRequest, VerificationResultResponse>
{
  public override void Configure()
  {
    Post("bikes/{id}/verification/possession");
    AllowFileUploads();
    Options(x => x.RequireRateLimiting(RateLimitPolicies.Ai));
    Summary(s => s.Summary = "Possession check (multipart file + code, header X-LeezenPass: 1)");
  }

  public override async Task HandleAsync(VerifyPossessionRequest req, CancellationToken ct)
  {
    // CSRF: a cross-site form post cannot set custom headers.
    if (HttpContext.Request.Headers["X-LeezenPass"] != "1")
    {
      await Send.ForbiddenAsync(ct);
      return;
    }

    var userId = User.GetUserId();
    var now = clock.UtcNow;
    var bike = await db.Bikes.FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var open = await db.PossessionChallenges
      .Where(c => c.BikeId == bike.Id && c.UserId == userId && c.UsedAt == null && c.ExpiresAt > now)
      .ToListAsync(ct);
    var challenge = open.FirstOrDefault(c => c.Matches(req.Code));
    if (challenge is null)
    {
      AddError(r => r.Code, "errors.challengeInvalid");
      await Send.ErrorsAsync(cancellation: ct);
      return;
    }

    byte[] forModel;
    try
    {
      await using var upload = req.File!.OpenReadStream();
      forModel = images.ToJpeg(upload, visionOptions.Value.MaxImageEdge);
    }
    catch (InvalidImageException)
    {
      AddError(r => r.File, "errors.unsupportedImage");
      await Send.ErrorsAsync(cancellation: ct);
      return;
    }

    PossessionReading reading;
    try
    {
      var hint = new VerificationHint(req.File.FileName, bike.FrameNoRaw, bike.Brand, bike.Model, req.Code);
      reading = await vision.CheckPossessionAsync(new MemoryStream(forModel), "image/jpeg", hint, ct);
    }
    catch (VisionUnavailableException ex)
    {
      logger.LogWarning(ex.InnerException, "Possession check unavailable: {Reason}", ex.Message);
      AddError("errors.aiUnavailable");
      await Send.ErrorsAsync(StatusCodes.Status503ServiceUnavailable, ct);
      return;
    }

    var outcome = VerificationRules.Possession(reading, req.Code, bike);
    if (outcome != CheckOutcome.Retry)
    {
      // Single use: a pass or a fail uses the code up; only an unclear photo (retry) keeps it.
      challenge.MarkUsed(now);

      // The model's reading without the code itself.
      var stored = reading with { CodeValue = null };
      var status = outcome == CheckOutcome.Passed ? EvidenceStatus.Passed : EvidenceStatus.Failed;
      var evidence = new OwnershipEvidence(bike.Id, userId, EvidenceKind.Possession, status, null, JsonSerializer.Serialize(stored), now);
      await db.RecordAsync(bike, userId, evidence, now, ct);
      await db.SaveChangesAsync(ct);
    }

    await Send.OkAsync(new VerificationResultResponse(outcome, await db.StatusAsync(bike, userId, now, ct)), ct);
  }
}
