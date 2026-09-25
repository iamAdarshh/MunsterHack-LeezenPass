using FluentValidation;
using LeezenPass.Api.Domain.Bikes;

namespace LeezenPass.Api.Features.Bikes;

/// <summary>Fields shared by register and update.</summary>
public interface IBikeDetailsRequest
{
  string FrameNumber { get; }
  string? FeinCode { get; }
  string? Brand { get; }
  string? Model { get; }
  BikeType Type { get; }
  string? ColorPrimary { get; }
  string? ColorSecondary { get; }
  bool IsEbike { get; }
  string? BatterySerial { get; }
  List<string> Features { get; }
  DateOnly? PurchaseDate { get; }
}

public static class BikeDetailsRequestExtensions
{
  public static BikeDetails ToDetails(this IBikeDetailsRequest r) => new(
    r.Brand, r.Model, r.Type, r.ColorPrimary, r.ColorSecondary, r.IsEbike, r.BatterySerial, r.Features, r.PurchaseDate);
}

/// <summary>Messages are i18n keys (errors.*); the web app translates them.</summary>
public class BikeDetailsValidator : AbstractValidator<IBikeDetailsRequest>
{
  public BikeDetailsValidator()
  {
    RuleFor(x => x.FrameNumber)
      .Must(f => Domain.Bikes.FrameNumber.TryCreate(f, out _))
      .WithMessage("errors.frameNumberInvalid");

    RuleFor(x => x.FeinCode)
      .Must(f => Domain.Bikes.FeinCode.TryCreate(f, out _))
      .When(x => !string.IsNullOrWhiteSpace(x.FeinCode))
      .WithMessage("errors.feinCodeInvalid");

    RuleFor(x => x.Brand).MaximumLength(60).WithMessage("errors.tooLong");
    RuleFor(x => x.Model).MaximumLength(60).WithMessage("errors.tooLong");
    RuleFor(x => x.BatterySerial).MaximumLength(64).WithMessage("errors.tooLong");
    RuleFor(x => x.Type).IsInEnum().WithMessage("errors.invalidValue");

    RuleFor(x => x.ColorPrimary)
      .NotEmpty().WithMessage("errors.required")
      .Must(c => c is null || BikeCatalog.Colors.Contains(c)).WithMessage("errors.invalidValue");
    RuleFor(x => x.ColorSecondary)
      .Must(c => BikeCatalog.Colors.Contains(c!))
      .When(x => !string.IsNullOrEmpty(x.ColorSecondary))
      .WithMessage("errors.invalidValue");

    RuleFor(x => x.Features)
      .Cascade(CascadeMode.Stop)
      .NotNull().WithMessage("errors.invalidValue")
      .Must(f => f.Count <= BikeCatalog.Features.Count && f.All(BikeCatalog.Features.Contains))
      .WithMessage("errors.invalidValue");

    RuleFor(x => x.PurchaseDate)
      .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
      .When(x => x.PurchaseDate is not null)
      .WithMessage("errors.dateInFuture");
  }
}
