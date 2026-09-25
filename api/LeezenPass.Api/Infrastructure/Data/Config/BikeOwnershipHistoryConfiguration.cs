using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeezenPass.Api.Infrastructure.Data.Config;

public class BikeOwnershipHistoryConfiguration : IEntityTypeConfiguration<BikeOwnershipHistory>
{
  public void Configure(EntityTypeBuilder<BikeOwnershipHistory> builder)
  {
    builder.ToTable("bike_ownership_history");
    builder.HasKey(h => h.Id);
    builder.HasIndex(h => h.BikeId);

    builder.HasOne<Bike>().WithMany(b => b.OwnershipHistory).HasForeignKey(h => h.BikeId).OnDelete(DeleteBehavior.Cascade);
    builder.HasOne<AppUser>().WithMany().HasForeignKey(h => h.OwnerId).OnDelete(DeleteBehavior.Cascade);
  }
}
