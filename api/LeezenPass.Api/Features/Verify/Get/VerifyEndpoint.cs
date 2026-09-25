using FastEndpoints;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Verify.Get;

public sealed record VerifyRequest
{
  public string Token { get; init; } = string.Empty;
}

/// <summary>
/// Public check of a transfer certificate. No identities; frame number only as a hint ("…5678")
/// to compare with the paper and the bike.
/// </summary>
public sealed record VerifyResponse(
  DateTimeOffset TransferredAt,
  BikeType Type,
  string? Brand,
  string? Model,
  string? ColorPrimary,
  string? ColorSecondary,
  string FrameNumberHint,
  bool StillWithThisOwner,
  BikeStatus CurrentStatus);

public class VerifyEndpoint(AppDbContext db) : Endpoint<VerifyRequest, VerifyResponse>
{
  public override void Configure()
  {
    Get("verify/{token}");
    AllowAnonymous();
    Summary(s => s.Summary = "Verify a transfer certificate (public)");
  }

  public override async Task HandleAsync(VerifyRequest req, CancellationToken ct)
  {
    var row = await db.OwnershipTransfers
      .Where(t => t.VerifyToken == req.Token && t.CompletedAt != null)
      .Join(db.Bikes, t => t.BikeId, b => b.Id, (t, b) => new
      {
        TransferredAt = t.CompletedAt!.Value,
        b.Type,
        b.Brand,
        b.Model,
        b.ColorPrimary,
        b.ColorSecondary,
        b.FrameNoNorm,
        StillWithThisOwner = b.OwnerId == t.ToUserId,
        b.Status,
      })
      .FirstOrDefaultAsync(ct);

    if (row is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    await Send.OkAsync(new VerifyResponse(
      row.TransferredAt, row.Type, row.Brand, row.Model, row.ColorPrimary, row.ColorSecondary,
      FrameNumber.Hint(row.FrameNoNorm), row.StillWithThisOwner, row.Status), ct);
  }
}
