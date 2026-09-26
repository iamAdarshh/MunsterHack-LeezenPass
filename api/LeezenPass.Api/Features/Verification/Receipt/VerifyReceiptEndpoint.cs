using System.Text.Json;
using FastEndpoints;
using FluentValidation;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Verification;
using LeezenPass.Api.Features.Bikes.UploadPhoto;
using LeezenPass.Api.Features.Goodwill;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Images;
using LeezenPass.Api.Infrastructure.Storage;
using LeezenPass.Api.Infrastructure.Time;
using LeezenPass.Api.Infrastructure.Vision;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Verification.Receipt;

public sealed record VerifyReceiptRequest
{
  public Guid Id { get; init; }
  public IFormFile? File { get; init; }
}

public class VerifyReceiptValidator : Validator<VerifyReceiptRequest>
{
  public VerifyReceiptValidator()
  {
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
/// Receipt check: the model reads the receipt, <see cref="VerificationRules.Receipt"/> decides. A passed receipt is
/// kept as the bike's receipt photo (owner-only, EXIF stripped, deleted on transfer); failed/unclear ones are not stored.
/// </summary>
public class VerifyReceiptEndpoint(
  AppDbContext db,
  IVisionExtractor vision,
  ImageProcessor images,
  IFileStorage storage,
  IClock clock,
  GoodwillService goodwill,
  IOptions<VisionOptions> visionOptions,
  ILogger<VerifyReceiptEndpoint> logger) : Endpoint<VerifyReceiptRequest, VerificationResultResponse>
{
  public override void Configure()
  {
    Post("bikes/{id}/verification/receipt");
    AllowFileUploads();
    Options(x => x.RequireRateLimiting(RateLimitPolicies.Ai));
    Summary(s => s.Summary = "Receipt check (multipart file, header X-LeezenPass: 1)");
  }

  public override async Task HandleAsync(VerifyReceiptRequest req, CancellationToken ct)
  {
    // CSRF: a cross-site form post cannot set custom headers.
    if (HttpContext.Request.Headers["X-LeezenPass"] != "1")
    {
      await Send.ForbiddenAsync(ct);
      return;
    }

    var userId = User.GetUserId();
    var bike = await db.Bikes.Include(b => b.Photos).FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    ProcessedImage processed;
    byte[] forModel;
    try
    {
      await using (var upload = req.File!.OpenReadStream())
      {
        processed = images.Process(upload);
      }

      forModel = images.ToJpeg(new MemoryStream(processed.Full), visionOptions.Value.MaxImageEdge);
    }
    catch (InvalidImageException)
    {
      AddError(r => r.File, "errors.unsupportedImage");
      await Send.ErrorsAsync(cancellation: ct);
      return;
    }

    ReceiptReading reading;
    try
    {
      var hint = new VerificationHint(req.File.FileName, bike.FrameNoRaw, bike.Brand, bike.Model, null);
      reading = await vision.ExtractReceiptAsync(new MemoryStream(forModel), "image/jpeg", hint, ct);
    }
    catch (VisionUnavailableException ex)
    {
      logger.LogWarning(ex.InnerException, "Receipt check unavailable: {Reason}", ex.Message);
      AddError("errors.aiUnavailable");
      await Send.ErrorsAsync(StatusCodes.Status503ServiceUnavailable, ct);
      return;
    }

    var now = clock.UtcNow;
    var outcome = VerificationRules.Receipt(reading, bike, DateOnly.FromDateTime(now.UtcDateTime));
    var points = 0;
    if (outcome != CheckOutcome.Retry)
    {
      Guid? photoId = null;
      if (outcome == CheckOutcome.Passed && bike.Photos.Count < BikeCatalog.MaxPhotosPerBike)
      {
        photoId = Guid.CreateVersion7();
        var key = PhotoStorageKeys.Full(bike.Id, photoId.Value);
        await storage.SaveAsync(key, new MemoryStream(processed.Full), ct);
        await storage.SaveAsync(PhotoStorageKeys.Thumbnail(key), new MemoryStream(processed.Thumbnail), ct);
        db.BikePhotos.Add(new BikePhoto(photoId.Value, bike.Id, PhotoKind.Receipt, key, now));
      }

      var status = outcome == CheckOutcome.Passed ? EvidenceStatus.Passed : EvidenceStatus.Failed;
      var evidence = new OwnershipEvidence(bike.Id, userId, EvidenceKind.Receipt, status, photoId, JsonSerializer.Serialize(reading), now);
      var raised = await db.RecordAsync(bike, userId, evidence, now, ct);
      points = await db.SaveCheckAsync(goodwill, bike, userId, raised, ct);
    }

    await Send.OkAsync(new VerificationResultResponse(outcome, await db.StatusAsync(bike, userId, now, ct), points), ct);
  }
}
