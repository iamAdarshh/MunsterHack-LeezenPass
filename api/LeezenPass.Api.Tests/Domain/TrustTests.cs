using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Transfers;
using LeezenPass.Api.Infrastructure.Seed;

namespace LeezenPass.Api.Tests.Domain;

public class TrustTests
{
  private static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

  private static Bike NewBike() => new(Guid.NewGuid(), FrameNumber.Create("WGZ12345678"), Now);

  [Fact]
  public void A_new_bike_is_self_declared()
  {
    var bike = NewBike();

    Assert.Equal(TrustLevel.SelfDeclared, bike.TrustLevel);
    Assert.Null(bike.TrustSource);
    Assert.Null(bike.TrustVerifiedAt);
  }

  [Fact]
  public void Evidence_check_raises_trust_and_records_source_and_time()
  {
    var bike = NewBike();

    bike.MarkEvidenceChecked(Now);

    Assert.Equal(TrustLevel.EvidenceChecked, bike.TrustLevel);
    Assert.Equal(TrustSource.ReceiptPossession, bike.TrustSource);
    Assert.Equal(Now, bike.TrustVerifiedAt);
  }

  [Fact]
  public void Evidence_check_never_lowers_a_partner_verified_bike()
  {
    var bike = NewBike();
    bike.MarkPartnerVerified(Now);

    bike.MarkEvidenceChecked(Now.AddDays(1));

    Assert.Equal(TrustLevel.ThirdPartyVerified, bike.TrustLevel);
    Assert.Equal(TrustSource.Partner, bike.TrustSource);
    Assert.Equal(Now, bike.TrustVerifiedAt);
  }

  [Fact]
  public void Transfer_keeps_the_trust_level()
  {
    var bike = NewBike();
    bike.MarkEvidenceChecked(Now);

    bike.TransferTo(Guid.NewGuid(), Now.AddDays(1));

    Assert.Equal(TrustLevel.EvidenceChecked, bike.TrustLevel);
  }

  [Fact]
  public void Demo_look_alike_is_a_loose_match_of_the_stolen_demo_bike()
  {
    var stolen = FrameNumber.Create(DemoScenario.StolenFrame);
    var typo = FrameNumber.Create(DemoScenario.LookAlikeFrame);

    Assert.NotEqual(stolen.Normalized, typo.Normalized);
    Assert.Equal(stolen.Loose, typo.Loose);
  }

  [Fact]
  public void Demo_transfer_code_is_valid()
  {
    Assert.True(TransferCode.TryParse(DemoScenario.TransferCode, out _));
  }
}
