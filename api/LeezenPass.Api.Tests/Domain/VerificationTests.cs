using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Verification;

namespace LeezenPass.Api.Tests.Domain;

public class VerificationTests
{
  private static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);
  private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

  private static Bike NewBike()
  {
    var bike = new Bike(Guid.NewGuid(), FrameNumber.Create("ST-8830-1145"), Now);
    bike.UpdateDetails(new BikeDetails("Stevens", "Courier Luxe", BikeType.City, "green", null, false, null, [], null));
    return bike;
  }

  [Theory]
  [InlineData("ST 8830 1145")]
  [InlineData("5T-883O-1145")] // look-alikes still match (loose key)
  public void Receipt_passes_when_the_frame_number_matches(string read)
  {
    var reading = new ReceiptReading(read, null, null, null, 0.9);

    Assert.Equal(CheckOutcome.Passed, VerificationRules.Receipt(reading, NewBike(), Today));
  }

  [Fact]
  public void Receipt_passes_on_brand_model_and_past_date_without_frame_number()
  {
    var reading = new ReceiptReading(null, "STEVENS", "Courier Lux", Today.AddDays(-100), 0.9);

    Assert.Equal(CheckOutcome.Passed, VerificationRules.Receipt(reading, NewBike(), Today));
  }

  [Fact]
  public void Receipt_fails_on_future_date_other_brand_or_other_frame_number()
  {
    var bike = NewBike();

    Assert.Equal(CheckOutcome.Failed, VerificationRules.Receipt(new ReceiptReading(null, "Stevens", "Courier Luxe", Today.AddDays(1), 0.9), bike, Today));
    Assert.Equal(CheckOutcome.Failed, VerificationRules.Receipt(new ReceiptReading(null, "Gazelle", "Courier Luxe", Today, 0.9), bike, Today));
    Assert.Equal(CheckOutcome.Failed, VerificationRules.Receipt(new ReceiptReading("WGA 1234 5678", null, null, null, 0.9), bike, Today));
  }

  [Fact]
  public void Unclear_photos_ask_for_a_retry_instead_of_failing()
  {
    var bike = NewBike();

    Assert.Equal(CheckOutcome.Retry, VerificationRules.Receipt(new ReceiptReading("ST-8830-1145", null, null, null, 0.5), bike, Today));
    Assert.Equal(CheckOutcome.Retry, VerificationRules.Possession(new PossessionReading(true, "1234", "ST-8830-1145", 0.3), "1234", bike));
    Assert.Equal(CheckOutcome.Retry, VerificationRules.Possession(new PossessionReading(false, null, "ST-8830-1145", 0.9), "1234", bike));
  }

  [Fact]
  public void Possession_needs_the_right_code_and_frame_number()
  {
    var bike = NewBike();

    Assert.Equal(CheckOutcome.Passed, VerificationRules.Possession(new PossessionReading(true, "0 4 2 7", "ST88301145", 0.9), "0427", bike));
    Assert.Equal(CheckOutcome.Failed, VerificationRules.Possession(new PossessionReading(true, "0428", "ST88301145", 0.9), "0427", bike));
    Assert.Equal(CheckOutcome.Failed, VerificationRules.Possession(new PossessionReading(true, "0427", "WGA12345678", 0.9), "0427", bike));
  }

  [Fact]
  public void Evidence_checked_needs_both_kinds()
  {
    Assert.False(VerificationRules.BothPassed([EvidenceKind.Receipt, EvidenceKind.Receipt]));
    Assert.True(VerificationRules.BothPassed([EvidenceKind.Possession, EvidenceKind.Receipt]));
  }

  [Fact]
  public void Challenge_is_four_digits_hashed_and_bound_to_bike_and_user()
  {
    var (bikeId, userId) = (Guid.NewGuid(), Guid.NewGuid());

    var (challenge, code) = PossessionChallenge.Create(bikeId, userId, Now);

    Assert.Matches("^[0-9]{4}$", code);
    Assert.DoesNotContain(code, challenge.CodeHash, StringComparison.Ordinal);
    Assert.True(challenge.Matches(code));
    Assert.NotEqual(PossessionChallenge.Hash(code, bikeId, Guid.NewGuid()), challenge.CodeHash);
    Assert.NotEqual(PossessionChallenge.Hash(code, Guid.NewGuid(), userId), challenge.CodeHash);
  }

  [Fact]
  public void Challenge_expires_after_ten_minutes_and_is_single_use()
  {
    var (challenge, _) = PossessionChallenge.Create(Guid.NewGuid(), Guid.NewGuid(), Now);

    Assert.True(challenge.IsOpenAt(Now.AddMinutes(9)));
    Assert.False(challenge.IsOpenAt(Now.AddMinutes(10)));

    challenge.MarkUsed(Now.AddMinutes(1));
    Assert.False(challenge.IsOpenAt(Now.AddMinutes(2)));
    Assert.Throws<InvalidOperationException>(() => challenge.MarkUsed(Now.AddMinutes(2)));
  }

  [Theory]
  [InlineData("Courier Luxe", "courier-luxe", true)]
  [InlineData("Courier Lux", "Courier Luxe", true)]
  [InlineData("Courrier Luxe", "Courier Luxe", true)]
  [InlineData("Endeavour 5", "e", false)] // short registered model must not match everything
  [InlineData("C7", "C8", false)]
  [InlineData("Orange C7", "Chamonix", false)]
  public void Model_match_is_fuzzy_but_not_for_short_names(string read, string registered, bool expected)
  {
    Assert.Equal(expected, VerificationRules.ModelMatches(read, registered));
  }

  [Fact]
  public void Frame_number_is_locked_once_ownership_is_proven()
  {
    var bike = NewBike();
    Assert.True(bike.CanChangeFrameNumber);

    bike.MarkEvidenceChecked(Now);

    Assert.False(bike.CanChangeFrameNumber);
  }
}
