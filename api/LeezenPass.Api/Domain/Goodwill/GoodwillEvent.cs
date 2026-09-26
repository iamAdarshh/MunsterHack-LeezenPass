namespace LeezenPass.Api.Domain.Goodwill;

/// <summary>
/// One line of the append-only goodwill ledger. Unique per (user, action, ref type, ref id), so crediting the same
/// outcome twice is a no-op. Revoked rows stay (with <see cref="RevokedAt"/>) and no longer count.
/// </summary>
public class GoodwillEvent
{
  private GoodwillEvent() { }

  public GoodwillEvent(Guid userId, GoodwillAction action, string refType, Guid refId, DateTimeOffset now)
  {
    Id = Guid.CreateVersion7();
    UserId = userId;
    Action = action;
    Points = GoodwillPolicy.Points(action);
    RefType = refType;
    RefId = refId;
    CreatedAt = now;
  }

  public Guid Id { get; private set; }
  public Guid UserId { get; private set; }
  public GoodwillAction Action { get; private set; }
  public int Points { get; private set; }
  public GoodwillStatus Status { get; private set; } = GoodwillStatus.Credited;

  /// <summary>What the points are for, e.g. "bike" + bike id.</summary>
  public string RefType { get; private set; } = string.Empty;

  public Guid RefId { get; private set; }
  public DateTimeOffset CreatedAt { get; private set; }
  public DateTimeOffset? RevokedAt { get; private set; }
}

public static class GoodwillRefTypes
{
  public const string Bike = "bike";

  /// <summary>
  /// Keyed by frame number instead of bike id: deleting and re-registering the same bike (new id) must not earn
  /// the verification points again.
  /// </summary>
  public const string Frame = "frame";

  /// <summary>Stable id for a normalised frame number (first 16 bytes of its SHA-256).</summary>
  public static Guid FrameId(string frameNoNorm) =>
    new(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(frameNoNorm)).AsSpan(0, 16));
}
