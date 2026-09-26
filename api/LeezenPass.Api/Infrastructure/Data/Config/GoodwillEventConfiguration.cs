using LeezenPass.Api.Domain.Goodwill;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeezenPass.Api.Infrastructure.Data.Config;

public class GoodwillEventConfiguration : IEntityTypeConfiguration<GoodwillEvent>
{
  public void Configure(EntityTypeBuilder<GoodwillEvent> builder)
  {
    builder.ToTable("goodwill_events");
    builder.HasKey(e => e.Id);
    builder.Property(e => e.RefType).HasMaxLength(20).IsRequired();
    // Idempotent ledger: the same outcome can be credited only once (GoodwillService uses ON CONFLICT DO NOTHING).
    builder.HasIndex(e => new { e.UserId, e.Action, e.RefType, e.RefId }).IsUnique();
    builder.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}
