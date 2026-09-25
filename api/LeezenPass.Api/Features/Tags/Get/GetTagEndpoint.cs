using FastEndpoints;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Tags.Get;

public sealed record GetTagRequest
{
  public string Token { get; init; } = string.Empty;
}

/// <summary>"registered" or "stolen". Nothing else: no attributes, no owner data.</summary>
public sealed record TagResponse(string Status);

/// <summary>
/// Data for the QR tag page (/b/{token}). 404 for unknown tokens, so a fake QR code sticker
/// can't make a stolen bike look "registered and clean".
/// </summary>
public class GetTagEndpoint(AppDbContext db) : Endpoint<GetTagRequest, TagResponse>
{
  public override void Configure()
  {
    Get("tags/{token}");
    AllowAnonymous();
    Summary(s => s.Summary = "Status behind a QR tag: registered | stolen");
  }

  public override async Task HandleAsync(GetTagRequest req, CancellationToken ct)
  {
    var status = await db.Bikes
      .Where(b => b.PublicToken == req.Token)
      .Select(b => (BikeStatus?)b.Status)
      .FirstOrDefaultAsync(ct);

    if (status is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    await Send.OkAsync(new TagResponse(status == BikeStatus.Stolen ? "stolen" : "registered"), ct);
  }
}
