using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Transfers;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeezenPass.Api.Infrastructure.Data.Config;

public class OwnershipTransferConfiguration : IEntityTypeConfiguration<OwnershipTransfer>
{
  public void Configure(EntityTypeBuilder<OwnershipTransfer> builder)
  {
    builder.ToTable("ownership_transfers");
    builder.HasKey(t => t.Id);

    builder.Property(t => t.FromUserId).HasColumnName("from_user");
    builder.Property(t => t.ToUserId).HasColumnName("to_user");
    builder.Property(t => t.CodeHash).HasMaxLength(64).IsRequired();

    builder.Property(t => t.VerifyToken).HasMaxLength(32);

    // Postgres xmin as optimistic concurrency token: two buyers racing for one code can't both win.
    builder.Property<uint>("Version").IsRowVersion();

    builder.HasIndex(t => t.CodeHash).IsUnique();
    builder.HasIndex(t => t.VerifyToken).IsUnique();
    builder.HasIndex(t => t.BikeId);

    builder.HasOne<Bike>().WithMany().HasForeignKey(t => t.BikeId).OnDelete(DeleteBehavior.Cascade);
    builder.HasOne<AppUser>().WithMany().HasForeignKey(t => t.FromUserId).OnDelete(DeleteBehavior.Cascade);
    builder.HasOne<AppUser>().WithMany().HasForeignKey(t => t.ToUserId).OnDelete(DeleteBehavior.SetNull);
  }
}
