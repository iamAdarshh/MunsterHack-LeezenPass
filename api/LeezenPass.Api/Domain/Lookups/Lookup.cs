namespace LeezenPass.Api.Domain.Lookups;

public enum LookupResult { Stolen, VerifiedTransfer, Unknown, PossibleMatch }

/// <summary>Audit trail of public checks. Hashes only: no IPs, no frame numbers in plain text.</summary>
public class Lookup
{
  private Lookup() { }

  public Lookup(string ipHash, string queryHash, LookupResult result, DateTimeOffset createdAt)
  {
    Id = Guid.CreateVersion7();
    IpHash = ipHash;
    QueryHash = queryHash;
    Result = result;
    CreatedAt = createdAt;
  }

  public Guid Id { get; private set; }
  public string IpHash { get; private set; } = string.Empty;
  public string QueryHash { get; private set; } = string.Empty;
  public LookupResult Result { get; private set; }
  public DateTimeOffset CreatedAt { get; private set; }
}
