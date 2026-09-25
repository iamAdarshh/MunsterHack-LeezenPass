using FastEndpoints;
using FluentValidation;
using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Features.Bikes.UploadPhoto;

public sealed record UploadPhotoRequest
{
  public Guid Id { get; init; }
  public IFormFile? File { get; init; }

  /// <summary>side | frame_no | detail | receipt</summary>
  public string Kind { get; init; } = string.Empty;
}

public static class PhotoKinds
{
  public static readonly IReadOnlyDictionary<string, PhotoKind> ByName = new Dictionary<string, PhotoKind>
  {
    ["side"] = PhotoKind.Side,
    ["frame_no"] = PhotoKind.FrameNo,
    ["detail"] = PhotoKind.Detail,
    ["receipt"] = PhotoKind.Receipt,
  };
}

public class UploadPhotoValidator : Validator<UploadPhotoRequest>
{
  public const long MaxBytes = 15 * 1024 * 1024;
  private static readonly HashSet<string> AllowedTypes = ["image/jpeg", "image/png", "image/webp"];

  public UploadPhotoValidator()
  {
    RuleFor(x => x.Kind).Must(PhotoKinds.ByName.ContainsKey).WithMessage("errors.invalidValue");
    RuleFor(x => x.File).NotNull().WithMessage("errors.required");
    RuleFor(x => x.File!.Length)
      .InclusiveBetween(1, MaxBytes).WithMessage("errors.fileTooLarge")
      .When(x => x.File is not null);
    RuleFor(x => x.File!.ContentType)
      .Must(AllowedTypes.Contains).WithMessage("errors.unsupportedImage")
      .When(x => x.File is not null);
  }
}
