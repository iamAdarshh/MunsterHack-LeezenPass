using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Theft;
using LeezenPass.Api.Domain.Transfers;

namespace LeezenPass.Api.Tests.Domain;

public class TransferTests
{
  private static readonly DateTimeOffset Now = new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);
  private static readonly Guid Seller = Guid.NewGuid();
  private static readonly Guid Buyer = Guid.NewGuid();

  private static Bike NewBike() => new(Seller, FrameNumber.Create("WGZ12345678"), Now.AddYears(-1));

  [Fact]
  public void Completing_sets_buyer_and_issues_a_verify_token()
  {
    var transfer = new OwnershipTransfer(Guid.NewGuid(), Seller, TransferCode.Generate(), Now);

    transfer.Complete(Buyer, Now.AddHours(1));

    Assert.Equal(Buyer, transfer.ToUserId);
    Assert.Equal(Now.AddHours(1), transfer.CompletedAt);
    Assert.Matches("^[A-Za-z0-9_-]{22}$", transfer.VerifyToken);
    Assert.False(transfer.IsOpenAt(Now.AddHours(1)));
  }

  [Fact]
  public void Code_is_single_use()
  {
    var transfer = new OwnershipTransfer(Guid.NewGuid(), Seller, TransferCode.Generate(), Now);
    transfer.Complete(Buyer, Now);

    Assert.Throws<InvalidOperationException>(() => transfer.Complete(Guid.NewGuid(), Now));
  }

  [Fact]
  public void Expired_code_cannot_be_claimed()
  {
    var transfer = new OwnershipTransfer(Guid.NewGuid(), Seller, TransferCode.Generate(), Now);

    Assert.Throws<InvalidOperationException>(() => transfer.Complete(Buyer, Now + TransferCode.Validity));
  }

  [Fact]
  public void Seller_cannot_claim_own_code()
  {
    var transfer = new OwnershipTransfer(Guid.NewGuid(), Seller, TransferCode.Generate(), Now);

    Assert.Throws<InvalidOperationException>(() => transfer.Complete(Seller, Now));
    Assert.True(transfer.IsOpenAt(Now));
  }

  [Fact]
  public void Cancelled_code_is_closed_immediately()
  {
    var transfer = new OwnershipTransfer(Guid.NewGuid(), Seller, TransferCode.Generate(), Now);

    transfer.Cancel(Now.AddMinutes(5));

    Assert.False(transfer.IsOpenAt(Now.AddMinutes(5)));
    Assert.Throws<InvalidOperationException>(() => transfer.Complete(Buyer, Now.AddMinutes(6)));
  }

  [Fact]
  public void New_bike_starts_its_ownership_history()
  {
    var bike = NewBike();

    var entry = Assert.Single(bike.OwnershipHistory);
    Assert.Equal((Seller, (DateTimeOffset?)null), (entry.OwnerId, entry.ToAt));
  }

  [Fact]
  public void Transfer_changes_owner_and_keeps_history()
  {
    var bike = NewBike();

    bike.TransferTo(Buyer, Now);

    Assert.Equal(Buyer, bike.OwnerId);
    Assert.Collection(
      bike.OwnershipHistory,
      first => Assert.Equal((Seller, (DateTimeOffset?)Now), (first.OwnerId, first.ToAt)),
      second => Assert.Equal((Buyer, Now, (DateTimeOffset?)null), (second.OwnerId, second.FromAt, second.ToAt)));
  }

  [Fact]
  public void Seller_data_does_not_travel_with_the_bike()
  {
    var bike = NewBike();
    Assert.True(FeinCode.TryCreate("MS 123 456 789 ABC", out var fein));
    bike.SetFeinCode(fein, "a-test-secret-that-is-long-enough-32b"u8);
    bike.Photos.Add(new BikePhoto(Guid.NewGuid(), bike.Id, PhotoKind.Side, "side.jpg", Now));
    bike.Photos.Add(new BikePhoto(Guid.NewGuid(), bike.Id, PhotoKind.Receipt, "receipt.jpg", Now));

    var removed = bike.TransferTo(Buyer, Now);

    Assert.Null(bike.FeinCodeHash);
    Assert.Equal("receipt.jpg", Assert.Single(removed).Path);
    Assert.Equal(PhotoKind.Side, Assert.Single(bike.Photos).Kind);
  }

  [Fact]
  public void Stolen_bike_cannot_be_transferred()
  {
    var bike = NewBike();
    bike.ReportStolen(Now, 51.96, 7.62, LockType.Chain, Now);

    Assert.False(bike.CanTransfer);
    Assert.Throws<InvalidOperationException>(() => bike.TransferTo(Buyer, Now));
  }

  [Fact]
  public void Recovered_bike_is_active_again_for_the_new_owner()
  {
    var bike = NewBike();
    bike.ReportStolen(Now, 51.96, 7.62, LockType.Chain, Now);
    bike.MarkRecovered();

    bike.TransferTo(Buyer, Now.AddDays(1));

    Assert.Equal(BikeStatus.Active, bike.Status);
  }

  [Fact]
  public void Cannot_transfer_to_current_owner()
  {
    Assert.Throws<InvalidOperationException>(() => NewBike().TransferTo(Seller, Now));
  }

  [Theory]
  [InlineData("WGZ12345678", "…5678")]
  [InlineData("AB12", "AB12")]
  public void Frame_number_hint_shows_only_last_four(string normalized, string expected)
  {
    Assert.Equal(expected, FrameNumber.Hint(normalized));
  }
}
