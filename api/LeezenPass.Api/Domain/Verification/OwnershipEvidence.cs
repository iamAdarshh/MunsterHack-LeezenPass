namespace LeezenPass.Api.Domain.Verification;

public enum EvidenceKind { Receipt, Possession }

public enum EvidenceStatus { Passed, Failed }

/// <summary>Result of one receipt or possession check. Owner-only; the photo may be gone after a transfer.</summary>
public class OwnershipEvidence
{
  private OwnershipEvidence() { }

  public OwnershipEvidence(Guid bikeId, Guid userId, EvidenceKind kind, EvidenceStatus status, Guid? photoId, string aiResult, DateTimeOffset now)
  {
    Id = Guid.CreateVersion7();
    BikeId = bikeId;
    UserId = userId;
    Kind = kind;
    Status = status;
    PhotoId = photoId;
    AiResult = aiResult;
    CreatedAt = now;
  }

  public Guid Id { get; private set; }
  public Guid BikeId { get; private set; }

  /// <summary>The owner who provided the evidence: a new owner's checks don't mix with the previous owner's.</summary>
  public Guid UserId { get; private set; }

  public EvidenceKind Kind { get; private set; }
  public EvidenceStatus Status { get; private set; }
  public Guid? PhotoId { get; private set; }

  /// <summary>What the model read, as JSON (our reading records only, never the raw model reply).</summary>
  public string AiResult { get; private set; } = "{}";

  public DateTimeOffset CreatedAt { get; private set; }
}
