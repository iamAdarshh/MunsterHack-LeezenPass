using FastEndpoints;
using FluentValidation;
using LeezenPass.Api.Domain.Theft;

namespace LeezenPass.Api.Features.Theft.Report;

public sealed record ReportTheftRequest
{
  public Guid Id { get; init; }
  public DateTimeOffset StolenAt { get; init; }
  public double Latitude { get; init; }
  public double Longitude { get; init; }
  public string? LocationNote { get; init; }
  public LockType LockType { get; init; } = LockType.Other;
  public string? PoliceCaseNo { get; init; }
}

public class ReportTheftValidator : Validator<ReportTheftRequest>
{
  public ReportTheftValidator()
  {
    RuleFor(x => x.StolenAt)
      .Must(t => t <= DateTimeOffset.UtcNow.AddMinutes(5)).WithMessage("errors.dateInFuture")
      .Must(t => t >= DateTimeOffset.UtcNow.AddYears(-1)).WithMessage("errors.dateTooOld");
    RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).WithMessage("errors.locationRequired");
    RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).WithMessage("errors.locationRequired");
    // (0, 0) is what an unset pin looks like.
    RuleFor(x => x).Must(x => x.Latitude != 0 || x.Longitude != 0)
      .WithName("latitude").WithMessage("errors.locationRequired");
    RuleFor(x => x.LockType).IsInEnum().WithMessage("errors.invalidValue");
    RuleFor(x => x.LocationNote).MaximumLength(TheftReport.MaxLocationNoteLength).WithMessage("errors.tooLong");
    RuleFor(x => x.PoliceCaseNo).MaximumLength(TheftReport.MaxPoliceCaseNoLength).WithMessage("errors.tooLong");
  }
}
