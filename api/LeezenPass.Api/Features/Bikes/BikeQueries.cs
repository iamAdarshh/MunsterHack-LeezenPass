using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Theft;
using Microsoft.EntityFrameworkCore;

namespace LeezenPass.Api.Features.Bikes;

public static class BikeQueries
{
  /// <summary>Everything <see cref="BikeResponse.From"/> needs: photos and the open theft report.</summary>
  public static IQueryable<Bike> WithDetails(this IQueryable<Bike> bikes) => bikes
    .Include(b => b.Photos)
    .Include(b => b.TheftReports.Where(t => t.Status == TheftReportStatus.Open))
    // Two collections: separate queries instead of one cartesian join.
    .AsSplitQuery();
}
