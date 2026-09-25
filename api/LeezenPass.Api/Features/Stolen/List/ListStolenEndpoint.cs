using FastEndpoints;
using FluentValidation;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Theft;
using LeezenPass.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Stolen.List;

public sealed record ListStolenRequest
{
  [QueryParam] public BikeType? Type { get; init; }
  [QueryParam] public string? Color { get; init; }
  [QueryParam] public string? District { get; init; }
}

public class ListStolenValidator : Validator<ListStolenRequest>
{
  public ListStolenValidator()
  {
    RuleFor(x => x.Color).Must(c => BikeCatalog.Colors.Contains(c!)).When(x => x.Color is not null)
      .WithMessage("errors.invalidValue");
    RuleFor(x => x.District).Must(d => MuensterDistricts.Keys.Contains(d!)).When(x => x.District is not null)
      .WithMessage("errors.invalidValue");
  }
}

/// <summary>
/// Public stolen list. Filters by district instead of a bounding box: a bbox over exact theft points could be
/// narrowed down until it reveals the exact spot.
/// </summary>
public class ListStolenEndpoint(AppDbContext db) : Endpoint<ListStolenRequest, List<StolenBikeResponse>>
{
  public const int MaxResults = 100;

  public override void Configure()
  {
    Get("stolen");
    AllowAnonymous();
    Summary(s => s.Summary = "Public list of currently stolen bikes (district only, no owner data)");
  }

  public override async Task HandleAsync(ListStolenRequest req, CancellationToken ct)
  {
    var rows = await db.StolenBikes(new StolenBikeFilter(Type: req.Type, Color: req.Color, District: req.District))
      .Take(MaxResults)
      .ToListAsync(ct);

    await Send.OkAsync(rows.Select(r => r.ToResponse()).ToList(), ct);
  }
}
