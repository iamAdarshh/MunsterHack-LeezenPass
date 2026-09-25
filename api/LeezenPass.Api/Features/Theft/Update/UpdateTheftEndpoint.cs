using FastEndpoints;
using FluentValidation;
using LeezenPass.Api.Domain.Theft;
using LeezenPass.Api.Features.Bikes;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Theft.Update;

public sealed record UpdateTheftRequest
{
  public Guid Id { get; init; }
  public string? LocationNote { get; init; }
  public string? PoliceCaseNo { get; init; }
}

public class UpdateTheftValidator : Validator<UpdateTheftRequest>
{
  public UpdateTheftValidator()
  {
    RuleFor(x => x.LocationNote).MaximumLength(TheftReport.MaxLocationNoteLength).WithMessage("errors.tooLong");
    RuleFor(x => x.PoliceCaseNo).MaximumLength(TheftReport.MaxPoliceCaseNoLength).WithMessage("errors.tooLong");
  }
}

/// <summary>Add the police case number (Aktenzeichen) once the owner has filed the report.</summary>
public class UpdateTheftEndpoint(AppDbContext db) : Endpoint<UpdateTheftRequest, BikeResponse>
{
  public override void Configure()
  {
    Put("bikes/{id}/theft");
    Summary(s => s.Summary = "Update note / police case number of the open theft report");
  }

  public override async Task HandleAsync(UpdateTheftRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var bike = await db.Bikes.WithDetails().FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike?.OpenTheftReport is not { } report)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    report.UpdateDetails(req.LocationNote, req.PoliceCaseNo);
    await db.SaveChangesAsync(ct);

    await Send.OkAsync(BikeResponse.From(bike), ct);
  }
}
