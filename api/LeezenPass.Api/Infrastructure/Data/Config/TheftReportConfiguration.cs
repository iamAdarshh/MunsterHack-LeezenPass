using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Theft;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeezenPass.Api.Infrastructure.Data.Config;

public class TheftReportConfiguration : IEntityTypeConfiguration<TheftReport>
{
  public void Configure(EntityTypeBuilder<TheftReport> builder)
  {
    builder.ToTable("theft_reports");
    builder.HasKey(t => t.Id);

    builder.Property(t => t.Location).HasColumnType("geography (point, 4326)").IsRequired();
    builder.Property(t => t.LocationNote).HasMaxLength(200);
    builder.Property(t => t.PoliceCaseNo).HasMaxLength(40);

    builder.HasIndex(t => t.Location).HasMethod("gist");
    builder.HasIndex(t => t.BikeId);

    builder.HasOne<Bike>().WithMany().HasForeignKey(t => t.BikeId).OnDelete(DeleteBehavior.Cascade);
  }
}
