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
    builder.Property(t => t.LocationNote).HasMaxLength(TheftReport.MaxLocationNoteLength);
    builder.Property(t => t.PoliceCaseNo).HasMaxLength(TheftReport.MaxPoliceCaseNoLength);
    builder.Property(t => t.District).HasMaxLength(30).IsRequired();
    builder.Ignore(t => t.Latitude);
    builder.Ignore(t => t.Longitude);
    builder.Ignore(t => t.IsPoliceReported);

    builder.HasIndex(t => t.Location).HasMethod("gist");
    builder.HasIndex(t => t.BikeId);
    builder.HasIndex(t => new { t.Status, t.StolenAt });
    // At most one open report per bike, even when two requests race (double tap).
    builder.HasIndex(t => t.BikeId, "OneOpenReportPerBike")
      .HasDatabaseName("ux_theft_reports_bike_id_open")
      .IsUnique()
      .HasFilter($"status = '{nameof(TheftReportStatus.Open)}'");

    builder.HasOne<Bike>().WithMany(b => b.TheftReports).HasForeignKey(t => t.BikeId).OnDelete(DeleteBehavior.Cascade);
  }
}
