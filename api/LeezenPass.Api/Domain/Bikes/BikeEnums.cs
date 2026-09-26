namespace LeezenPass.Api.Domain.Bikes;

public enum BikeStatus { Active, Stolen, Recovered }

public enum BikeType { City, Trekking, Mountain, Road, Gravel, Cargo, Folding, Kids, Other }

public enum PhotoKind { Side, FrameNo, Detail, Receipt }

/// <summary>How much LeezenPass trusts that the owner really owns the bike (SPEC feature 5).</summary>
public enum TrustLevel { SelfDeclared, EvidenceChecked, ThirdPartyVerified }

/// <summary>What raised the trust level above <see cref="TrustLevel.SelfDeclared"/>.</summary>
public enum TrustSource { ReceiptPossession, Partner, Transfer }
