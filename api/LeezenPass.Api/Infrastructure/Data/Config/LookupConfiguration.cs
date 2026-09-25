using LeezenPass.Api.Domain.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeezenPass.Api.Infrastructure.Data.Config;

public class LookupConfiguration : IEntityTypeConfiguration<Lookup>
{
  public void Configure(EntityTypeBuilder<Lookup> builder)
  {
    builder.ToTable("lookups");
    builder.HasKey(l => l.Id);
    builder.Property(l => l.IpHash).HasMaxLength(64).IsRequired();
    builder.Property(l => l.QueryHash).HasMaxLength(64).IsRequired();
    builder.HasIndex(l => l.CreatedAt);
  }
}
