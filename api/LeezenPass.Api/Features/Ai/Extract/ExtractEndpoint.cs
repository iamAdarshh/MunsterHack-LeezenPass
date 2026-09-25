using FastEndpoints;
using FluentValidation;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Features.Bikes.UploadPhoto;
using LeezenPass.Api.Infrastructure.Images;
using LeezenPass.Api.Infrastructure.Vision;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Ai.Extract;

public sealed record ExtractRequest
{
  public IFormFile? File { get; init; }
}

public class ExtractValidator : Validator<ExtractRequest>
{
  public ExtractValidator()
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
/// Photo -> suggested attributes. Suggestions only; nothing is saved and the image is not stored.
/// The image is re-encoded first (upright, no EXIF/GPS, max 1024 px) before it goes to the model.
/// 503 when the model is unavailable or too slow: the UI falls back to manual entry.
/// </summary>
public class ExtractEndpoint(
  IVisionExtractor vision,
  ImageProcessor images,
  IOptions<VisionOptions> options,
  ILogger<ExtractEndpoint> logger) : Endpoint<ExtractRequest, VisionSuggestion>
{
  public override void Configure()
  {
    Post("ai/extract");
    AllowFileUploads();
    Options(x => x.RequireRateLimiting(RateLimitPolicies.Ai));
    Summary(s => s.Summary = "Suggest bike attributes from a photo (multipart, header X-LeezenPass: 1)");
  }

  public override async Task HandleAsync(ExtractRequest req, CancellationToken ct)
  {
    // CSRF: a cross-site form post cannot set custom headers.
    if (HttpContext.Request.Headers["X-LeezenPass"] != "1")
    {
      await Send.ForbiddenAsync(ct);
      return;
    }

    byte[] jpeg;
    try
    {
      await using var upload = req.File!.OpenReadStream();
      jpeg = images.ToJpeg(upload, options.Value.MaxImageEdge);
    }
    catch (InvalidImageException)
    {
      AddError(r => r.File, "errors.unsupportedImage");
      await Send.ErrorsAsync(cancellation: ct);
      return;
    }

    try
    {
      var suggestion = await vision.ExtractAsync(new MemoryStream(jpeg), "image/jpeg", ct);
      await Send.OkAsync(suggestion, ct);
    }
    catch (VisionUnavailableException ex)
    {
      logger.LogWarning(ex.InnerException, "AI extraction unavailable: {Reason}", ex.Message);
      AddError("errors.aiUnavailable");
      await Send.ErrorsAsync(StatusCodes.Status503ServiceUnavailable, ct);
    }
  }
}
