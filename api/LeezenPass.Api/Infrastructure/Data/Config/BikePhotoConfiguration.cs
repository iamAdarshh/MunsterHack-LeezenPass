using LeezenPass.Api.Domain.Bikes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeezenPass.Api.Infrastructure.Data.Config;

public class BikePhotoConfiguration : IEntityTypeConfiguration<BikePhoto>
{
  public void Configure(EntityTypeBuilder<BikePhoto> builder)
  {
    builder.ToTable("bike_photos");
    builder.HasKey(p => p.Id);
    builder.Property(p => p.Path).HasMaxLength(256).IsRequired();
  }
}
