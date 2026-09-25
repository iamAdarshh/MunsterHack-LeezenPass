using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeezenPass.Api.Infrastructure.Data.Config;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
  public void Configure(EntityTypeBuilder<AppUser> builder)
  {
    builder.ToTable("users");
    builder.Property(u => u.DisplayName).HasMaxLength(100);
  }
}
