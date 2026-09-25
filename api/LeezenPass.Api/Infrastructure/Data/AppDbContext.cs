using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Lookups;
using LeezenPass.Api.Domain.Theft;
using LeezenPass.Api.Domain.Transfers;
using LeezenPass.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
  : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
  public DbSet<Bike> Bikes => Set<Bike>();
  public DbSet<BikePhoto> BikePhotos => Set<BikePhoto>();
  public DbSet<BikeOwnershipHistory> BikeOwnershipHistory => Set<BikeOwnershipHistory>();
  public DbSet<TheftReport> TheftReports => Set<TheftReport>();
  public DbSet<OwnershipTransfer> OwnershipTransfers => Set<OwnershipTransfer>();
  public DbSet<Lookup> Lookups => Set<Lookup>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.HasPostgresExtension("postgis");

    // Identity sets PascalCase "AspNet*" table names explicitly; keep everything snake_case.
    modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
    modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
    modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
    modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
    modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
  }

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    // Enums are stored as strings.
    configurationBuilder.Properties<BikeStatus>().HaveConversion<string>().HaveMaxLength(20);
    configurationBuilder.Properties<BikeType>().HaveConversion<string>().HaveMaxLength(20);
    configurationBuilder.Properties<PhotoKind>().HaveConversion<string>().HaveMaxLength(20);
    configurationBuilder.Properties<TheftReportStatus>().HaveConversion<string>().HaveMaxLength(20);
    configurationBuilder.Properties<LockType>().HaveConversion<string>().HaveMaxLength(20);
    configurationBuilder.Properties<LookupResult>().HaveConversion<string>().HaveMaxLength(20);
  }
}
