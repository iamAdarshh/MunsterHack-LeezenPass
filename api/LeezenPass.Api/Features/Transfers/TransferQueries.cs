using LeezenPass.Api.Domain.Transfers;

namespace LeezenPass.Api.Features.Transfers;

public static class TransferQueries
{
  /// <summary>Codes that can still be claimed (same rule as <see cref="OwnershipTransfer.IsOpenAt"/>, as SQL).</summary>
  public static IQueryable<OwnershipTransfer> OpenAt(this IQueryable<OwnershipTransfer> transfers, DateTimeOffset now) =>
    transfers.Where(t => t.CompletedAt == null && t.ExpiresAt > now);

  /// <summary>"ABCDEFGH" -> "ABCD-EFGH": easier to read out and type.</summary>
  public static string Format(TransferCode code) => $"{code.Value[..4]}-{code.Value[4..]}";
}
