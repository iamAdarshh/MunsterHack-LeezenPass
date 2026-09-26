namespace LeezenPass.Api.Infrastructure.Seed;

public static class SeedCommand
{
  /// <summary>Runs <see cref="DemoSeeder"/> and prints what the presenter needs on stage.</summary>
  public static async Task SeedDemoDataAsync(this WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var samples = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "seed", "photos"));
    var result = await scope.ServiceProvider.GetRequiredService<DemoSeeder>().RunAsync(samples, app.Lifetime.ApplicationStopping);

    Console.WriteLine($"""

      Demo-Daten seeded: {result.Users} users, {result.Bikes} bikes, {result.TheftReports} theft reports ({result.OpenTheftReports} open), {result.Photos} photos.
      Trust levels: {string.Join(", ", result.TrustLevels.Select(t => $"{t.Key} {t.Value}"))}

      Log in (password {DemoScenario.Password}): {DemoScenario.Owner}, {DemoScenario.Buyer}, {DemoScenario.Partner} (Partner), {DemoScenario.Admin} (Admin)

      Check demo:
        {DemoScenario.StolenFrame,-16} stolen
        {DemoScenario.LookAlikeFrame,-16} possible_match (O→0, B→8 typo of the stolen bike)
        {DemoScenario.TransferFrame,-16} verified_transfer (FEIN {DemoScenario.TransferFein})
        {DemoScenario.CleanFrame,-16} unknown, registered (FEIN {DemoScenario.CleanFein})
        {DemoScenario.UnregisteredFrame,-16} unknown, not registered

      Transfer code for {DemoScenario.Buyer}: {DemoScenario.TransferCode} (expires {result.TransferExpiresAt.ToLocalTime():ddd dd.MM. HH:mm}; re-seed before the pitch)
      Sample photos: {samples} ({string.Join(", ", result.SamplePhotos)})
      """);
  }
}
