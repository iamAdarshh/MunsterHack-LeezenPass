using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeezenPass.Api.Infrastructure.Data.Config;

public class BikeConfiguration : IEntityTypeConfiguration<Bike>
{
  public void Configure(EntityTypeBuilder<Bike> builder)
  {
    builder.ToTable("bikes");
    builder.HasKey(b => b.Id);

    builder.Property(b => b.PublicToken).HasMaxLength(32).IsRequired();
    builder.Property(b => b.FrameNoRaw).HasMaxLength(FrameNumber.MaxRawLength).IsRequired();
    builder.Property(b => b.FrameNoNorm).HasMaxLength(FrameNumber.MaxLength).IsRequired();
    builder.Property(b => b.FrameNoLoose).HasMaxLength(FrameNumber.MaxLength).IsRequired();
    builder.Property(b => b.FeinCodeHash).HasMaxLength(64);
    builder.Property(b => b.Brand).HasMaxLength(60);
    builder.Property(b => b.Model).HasMaxLength(60);
    builder.Property(b => b.ColorPrimary).HasMaxLength(30);
    builder.Property(b => b.ColorSecondary).HasMaxLength(30);
    builder.Property(b => b.BatterySerial).HasMaxLength(64);

    builder.HasIndex(b => b.PublicToken).IsUnique();
    builder.HasIndex(b => b.FrameNoNorm).IsUnique();
    builder.HasIndex(b => b.FrameNoLoose);
    builder.HasIndex(b => b.FeinCodeHash);
    builder.HasIndex(b => b.OwnerId);

    builder.HasOne<AppUser>().WithMany().HasForeignKey(b => b.OwnerId).OnDelete(DeleteBehavior.Cascade);
    // xmin as concurrency token: a claim can't silently overwrite a theft report written at the same moment.
    builder.Property<uint>("Version").IsRowVersion();

    builder.Ignore(b => b.OpenTheftReport);
    builder.Ignore(b => b.CanReportStolen);
    builder.Ignore(b => b.CanMarkRecovered);
    builder.Ignore(b => b.CanTransfer);
    builder.Ignore(b => b.CanChangeFrameNumber);
    builder.HasMany(b => b.Photos).WithOne().HasForeignKey(p => p.BikeId).OnDelete(DeleteBehavior.Cascade);
  }
}
