using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Verification;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeezenPass.Api.Infrastructure.Data.Config;

public class PossessionChallengeConfiguration : IEntityTypeConfiguration<PossessionChallenge>
{
  public void Configure(EntityTypeBuilder<PossessionChallenge> builder)
  {
    builder.ToTable("possession_challenges");
    builder.HasKey(c => c.Id);
    builder.Property(c => c.CodeHash).HasMaxLength(64).IsRequired();
    builder.HasIndex(c => new { c.BikeId, c.UserId });
    builder.HasOne<Bike>().WithMany().HasForeignKey(c => c.BikeId).OnDelete(DeleteBehavior.Cascade);
    builder.HasOne<AppUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}

public class OwnershipEvidenceConfiguration : IEntityTypeConfiguration<OwnershipEvidence>
{
  public void Configure(EntityTypeBuilder<OwnershipEvidence> builder)
  {
    builder.ToTable("ownership_evidence");
    builder.HasKey(e => e.Id);
    builder.Property(e => e.AiResult).HasColumnType("jsonb").IsRequired();
    builder.HasIndex(e => new { e.BikeId, e.UserId });
    builder.HasOne<Bike>().WithMany().HasForeignKey(e => e.BikeId).OnDelete(DeleteBehavior.Cascade);
    builder.HasOne<AppUser>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}
