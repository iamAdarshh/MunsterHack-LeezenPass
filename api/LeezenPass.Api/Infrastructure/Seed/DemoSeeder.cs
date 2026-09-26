using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Domain.Theft;
using LeezenPass.Api.Domain.Transfers;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Images;
using LeezenPass.Api.Infrastructure.Storage;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Infrastructure.Seed;

/// <summary>
/// Synthetic demo data (SPEC "Seed data"): demo users, ~200 bikes, 60 theft reports across Münster, one bike per
/// trust level and the fixed check-demo bikes from <see cref="DemoScenario"/>.
/// Re-runnable: first removes everything that belongs to *@demo.local users, then seeds again with the same
/// random seed, so the data is the same every time. Never touches other users' data.
/// </summary>
public class DemoSeeder(
  AppDbContext db,
  UserManager<AppUser> users,
  RoleManager<IdentityRole<Guid>> roles,
  IFileStorage storage,
  ImageProcessor images,
  IClock clock,
  IOptions<FeinOptions> fein,
  IOptions<FeaturesOptions> features,
  ILogger<DemoSeeder> logger)
{
  public const int BackgroundUsers = 40;
  public const int BackgroundBikes = 196;
  public const int OpenTheftReports = 47;
  public const int RecoveredTheftReports = 12;

  private static readonly (string Brand, string Model, BikeType Type)[] Models =
  [
    ("Gazelle", "Orange C7", BikeType.City), ("Gazelle", "Chamonix", BikeType.Trekking),
    ("Batavus", "Finez", BikeType.City), ("Kalkhoff", "Endeavour 5", BikeType.Trekking),
    ("Cube", "Kathmandu", BikeType.Trekking), ("Cube", "Attention", BikeType.Mountain),
    ("Stevens", "Courier Luxe", BikeType.City), ("Specialized", "Allez", BikeType.Road),
    ("Canyon", "Grizl", BikeType.Gravel), ("Riese & Müller", "Load 75", BikeType.Cargo),
    ("Babboe", "Big", BikeType.Cargo), ("Brompton", "C Line", BikeType.Folding),
    ("Puky", "Youke 16", BikeType.Kids), ("Hercules", "Roberta", BikeType.City),
    ("Pegasus", "Premio", BikeType.Trekking), ("Diamant", "Topas", BikeType.Trekking),
    ("Rose", "Backroad", BikeType.Gravel), ("Koga", "Miyata", BikeType.Trekking),
    ("Velo de Ville", "A200", BikeType.City), ("Urban Arrow", "Family", BikeType.Cargo),
  ];

  // Weighted: black and grey bikes dominate Münster's racks.
  private static readonly string[] Colors =
  [
    "black", "black", "black", "black", "black", "grey", "grey", "silver", "silver", "white", "white",
    "blue", "blue", "blue", "green", "green", "red", "red", "beige", "brown", "yellow", "orange", "pink", "purple",
  ];

  // No O, I, L, S, B, Z: the loose key can't turn a generated number into one of the fixed demo numbers.
  private static readonly string[] FramePrefixes = ["WGA", "KHT", "CKM", "PGN", "RMT", "HWC", "VTX", "EMR", "NRD", "DMT"];

  /// <summary>Theft hotspots (Hbf, Schloss, Prinzipalmarkt, Hafen, Aasee): clustered points for the risk map.</summary>
  private static readonly (double Lat, double Lon)[] Hotspots =
  [
    (51.9567, 7.6355), (51.9636, 7.6131), (51.9620, 7.6280), (51.9505, 7.6425), (51.9555, 7.6100),
  ];

  private static readonly string[] BusyDistricts =
  [
    "altstadt", "altstadt", "kreuzviertel", "kreuzviertel", "hansaviertel", "mauritz", "suedviertel",
    "geist", "aaseestadt", "gievenbeck", "kinderhaus", "hiltrup", "sentrup", "coerde", "gremmendorf",
  ];

  private readonly Random _random = new(2026);
  private readonly Dictionary<BikeLook, byte[]> _imageCache = [];
  private readonly List<string> _storedKeys = [];

  public async Task<DemoSeedResult> RunAsync(string samplePhotoDirectory, CancellationToken ct)
  {
    if (!features.Value.DemoMode)
    {
      throw new InvalidOperationException("Refusing to seed: Features:DemoMode is false. Demo data only goes into demo instances.");
    }

    await db.Database.MigrateAsync(ct);
    await RemovePreviousDemoDataAsync(ct);
    await EnsureRolesAsync();

    try
    {
      return await SeedAsync(samplePhotoDirectory, ct);
    }
    catch
    {
      // Photo files of bikes that never reached the database would be orphans; users are removed on the next run.
      foreach (var key in _storedKeys)
      {
        await storage.DeleteAsync(key, CancellationToken.None);
      }

      throw;
    }
  }

  private async Task<DemoSeedResult> SeedAsync(string samplePhotoDirectory, CancellationToken ct)
  {

    var now = clock.UtcNow;
    var owner = await CreateUserAsync(DemoScenario.Owner, "Demo-Daten: Besitzer:in", ct);
    await CreateUserAsync(DemoScenario.Buyer, "Demo-Daten: Käufer:in", ct);
    var partner = await CreateUserAsync(DemoScenario.Partner, "Demo-Daten: Fahrradladen", ct, AppRoles.Partner);
    await CreateUserAsync(DemoScenario.Admin, "Demo-Daten: Admin", ct, AppRoles.Admin);

    var background = new List<AppUser>();
    for (var i = 1; i <= BackgroundUsers; i++)
    {
      background.Add(await CreateUserAsync($"nutzer{i:00}@{DemoScenario.EmailDomain}", $"Demo-Daten: Nutzer:in {i:00}", ct));
    }

    var taken = await db.Bikes.Select(b => b.FrameNoNorm).ToHashSetAsync(ct);
    var bikes = new List<Bike>();

    // Fixed check-demo bikes: one per trust level, a stolen one and one with an open transfer.
    var stolen = NamedBike(owner, DemoScenario.StolenFrame, "Gazelle", "Orange C7", BikeType.City, "blue", "white", now.AddDays(-400), taken);
    stolen.Features = ["basket", "fenders", "hub_dynamo", "kickstand", "rack"];
    stolen.MarkEvidenceChecked(now.AddDays(-300));
    var stolenReport = stolen.ReportStolen(now.AddDays(-2).AddHours(-5), Hotspots[0].Lat + 0.0004, Hotspots[0].Lon + 0.0006, LockType.Cable, now.AddDays(-2));
    stolenReport.UpdateDetails("Demo-Daten: Fahrradständer Hbf Ostseite", "DEMO-2026-0815");
    bikes.Add(stolen);

    var forSale = NamedBike(owner, DemoScenario.TransferFrame, "Kalkhoff", "Endeavour 5", BikeType.Trekking, "grey", "black", now.AddDays(-700), taken);
    forSale.IsEbike = true;
    forSale.BatterySerial = "DEMO-BAT-5521";
    forSale.Features = ["disc_brakes", "fenders", "hub_dynamo", "lights", "rack"];
    forSale.SetFeinCode(ParseFein(DemoScenario.TransferFein), fein.Value.SecretBytes());
    forSale.MarkEvidenceChecked(now.AddDays(-600));
    bikes.Add(forSale);

    var clean = NamedBike(owner, DemoScenario.CleanFrame, "Stevens", "Courier Luxe", BikeType.City, "green", null, now.AddDays(-90), taken);
    clean.Features = ["bell", "fenders", "kickstand", "lights"];
    clean.SetFeinCode(ParseFein(DemoScenario.CleanFein), fein.Value.SecretBytes());
    bikes.Add(clean);

    var partnerBike = NamedBike(partner, DemoScenario.PartnerFrame, "Riese & Müller", "Charger4", BikeType.Trekking, "black", "silver", now.AddDays(-3), taken);
    partnerBike.IsEbike = true;
    partnerBike.Features = ["disc_brakes", "fenders", "lights", "rack", "suspension_fork"];
    partnerBike.MarkPartnerVerified(now.AddDays(-3));
    bikes.Add(partnerBike);

    foreach (var bike in bikes)
    {
      await AddPhotoAsync(bike, PhotoKind.Side, now, ct);
    }

    await AddPhotoAsync(stolen, PhotoKind.Detail, now, ct);
    foreach (var bike in new[] { stolen, forSale, clean })
    {
      await AddReceiptAsync(bike, now, ct);
    }

    // Background bikes; the first ones are stolen, the next ones were stolen and recovered.
    for (var i = 0; i < BackgroundBikes; i++)
    {
      var bike = BackgroundBike(background[i % background.Count], now, taken);
      if (i < OpenTheftReports + RecoveredTheftReports)
      {
        var (lat, lon) = TheftLocation();
        var stolenAt = i < OpenTheftReports
          ? now.AddDays(-_random.Next(0, 75)).AddMinutes(-_random.Next(30, 1400))
          : now.AddDays(-_random.Next(80, 300));
        var report = bike.ReportStolen(stolenAt, lat, lon, Pick(Enum.GetValues<LockType>()), stolenAt.AddHours(_random.Next(1, 20)));
        report.UpdateDetails("Demo-Daten", _random.Next(2) == 0 ? $"DEMO-{stolenAt:yyyy}-{_random.Next(1000, 9999)}" : null);
        await AddPhotoAsync(bike, PhotoKind.Side, now, ct);
        if (i >= OpenTheftReports)
        {
          bike.MarkRecovered();
        }
      }

      bikes.Add(bike);
    }

    db.Bikes.AddRange(bikes);
    var code = Domain.Transfers.TransferCode.TryParse(DemoScenario.TransferCode, out var parsed)
      ? parsed
      : throw new InvalidOperationException("DemoScenario.TransferCode is not a valid transfer code.");
    var transfer = new OwnershipTransfer(forSale.Id, owner.Id, code, now);
    db.OwnershipTransfers.Add(transfer);
    await db.SaveChangesAsync(ct);

    var samples = await WriteSamplePhotosAsync(samplePhotoDirectory, ct);

    var result = new DemoSeedResult(
      Users: 4 + BackgroundUsers,
      Bikes: bikes.Count,
      OpenTheftReports: bikes.Count(b => b.Status == BikeStatus.Stolen),
      TheftReports: bikes.Sum(b => b.TheftReports.Count),
      Photos: bikes.Sum(b => b.Photos.Count),
      TrustLevels: bikes.GroupBy(b => b.TrustLevel).ToDictionary(g => g.Key, g => g.Count()),
      TransferExpiresAt: transfer.ExpiresAt,
      SamplePhotos: samples);
    logger.LogInformation("Demo data seeded: {Bikes} bikes, {Reports} theft reports, {Photos} photos", result.Bikes, result.TheftReports, result.Photos);
    return result;
  }

  /// <summary>
  /// Everything owned by, or once owned by, a demo user: that is exactly what the seed created (a real user may
  /// have claimed a demo bike). Cascades remove photos rows, theft reports, history and transfers.
  /// </summary>
  private async Task RemovePreviousDemoDataAsync(CancellationToken ct)
  {
    var suffix = "@" + DemoScenario.EmailDomain;
    var demoUserIds = await db.Users.Where(u => u.NormalizedEmail!.EndsWith(suffix.ToUpperInvariant())).Select(u => u.Id).ToListAsync(ct);
    var demoBikes = db.Bikes.Where(b => demoUserIds.Contains(b.OwnerId) || b.OwnershipHistory.Any(h => demoUserIds.Contains(h.OwnerId)));

    var photoKeys = await demoBikes.SelectMany(b => b.Photos).Select(p => p.Path).ToListAsync(ct);
    foreach (var key in photoKeys)
    {
      await storage.DeleteAsync(key, ct);
      await storage.DeleteAsync(PhotoStorageKeys.Thumbnail(key), ct);
    }

    var removedBikes = await demoBikes.ExecuteDeleteAsync(ct);
    var removedUsers = await db.Users.Where(u => demoUserIds.Contains(u.Id)).ExecuteDeleteAsync(ct);
    logger.LogInformation("Removed previous demo data: {Users} users, {Bikes} bikes, {Photos} photos", removedUsers, removedBikes, photoKeys.Count);
  }

  private async Task EnsureRolesAsync()
  {
    foreach (var role in AppRoles.All)
    {
      if (!await roles.RoleExistsAsync(role))
      {
        Check(await roles.CreateAsync(new IdentityRole<Guid>(role)), role);
      }
    }
  }

  private async Task<AppUser> CreateUserAsync(string email, string displayName, CancellationToken ct, string? role = null)
  {
    ct.ThrowIfCancellationRequested();
    var user = new AppUser
    {
      UserName = email,
      Email = email,
      EmailConfirmed = true,
      DisplayName = displayName,
      CreatedAt = clock.UtcNow.AddDays(-_random.Next(30, 500)),
    };
    Check(await users.CreateAsync(user, DemoScenario.Password), email);
    if (role is not null)
    {
      Check(await users.AddToRoleAsync(user, role), email);
    }

    return user;
  }

  private static Bike NamedBike(AppUser owner, string frame, string brand, string model, BikeType type, string color, string? secondColor, DateTimeOffset createdAt, HashSet<string> taken)
  {
    var frameNumber = FrameNumber.Create(frame);
    if (!taken.Add(frameNumber.Normalized))
    {
      throw new InvalidOperationException($"Demo frame number {frame} is already registered by a non-demo user. Delete that bike first.");
    }

    var bike = new Bike(owner.Id, frameNumber, createdAt);
    bike.UpdateDetails(new BikeDetails(brand, model, type, color, secondColor, false, null, [], DateOnly.FromDateTime(createdAt.AddDays(-5).UtcDateTime)));
    return bike;
  }

  private Bike BackgroundBike(AppUser owner, DateTimeOffset now, HashSet<string> taken)
  {
    FrameNumber frameNumber;
    do
    {
      frameNumber = FrameNumber.Create($"{Pick(FramePrefixes)}{_random.Next(10_000_000, 99_999_999)}");
    }
    while (!taken.Add(frameNumber.Normalized));

    var (brand, model, type) = Pick(Models);
    var color = Pick(Colors);
    var second = _random.Next(3) == 0 ? Pick(Colors) : null;
    var isEbike = _random.NextDouble() < type switch { BikeType.Cargo => 0.7, BikeType.Trekking => 0.4, BikeType.City => 0.25, _ => 0.05 };
    string[] typical = type switch
    {
      BikeType.City => ["fenders", "rack", "kickstand", "lights", "bell", "basket", "coaster_brake", "hub_dynamo", "frame_lock", "child_seat"],
      BikeType.Trekking => ["fenders", "rack", "kickstand", "lights", "hub_dynamo", "disc_brakes", "suspension_fork", "frame_lock"],
      BikeType.Cargo => ["kickstand", "lights", "frame_lock", "disc_brakes", "child_seat"],
      BikeType.Mountain => ["suspension_fork", "disc_brakes"],
      _ => ["lights", "bell", "kickstand"],
    };
    var features = typical.Where(_ => _random.Next(2) == 0).ToList();

    var createdAt = now.AddDays(-_random.Next(100, 700));
    var bike = new Bike(owner.Id, frameNumber, createdAt);
    bike.UpdateDetails(new BikeDetails(brand, model, type, color, second, isEbike,
      isEbike ? $"DEMO-BAT-{_random.Next(1000, 9999)}" : null, features,
      _random.Next(4) == 0 ? null : DateOnly.FromDateTime(createdAt.AddDays(-_random.Next(1, 900)).UtcDateTime)));

    // Roughly 70 % self-declared, 20 % evidence-checked, 10 % registered by a partner.
    var trust = _random.NextDouble();
    if (trust < 0.1)
    {
      bike.MarkPartnerVerified(createdAt);
    }
    else if (trust < 0.3)
    {
      bike.MarkEvidenceChecked(createdAt.AddDays(_random.Next(1, 60)));
    }

    return bike;
  }

  /// <summary>40 % clustered at hotspots (±150 m), the rest spread over busy districts (±500 m).</summary>
  private (double Lat, double Lon) TheftLocation()
  {
    if (_random.NextDouble() < 0.4)
    {
      var (lat, lon) = Pick(Hotspots);
      return (lat + Jitter(0.0013), lon + Jitter(0.002));
    }

    var key = Pick(BusyDistricts);
    var district = MuensterDistricts.All.First(d => d.Key == key);
    return (district.Latitude + Jitter(0.0045), district.Longitude + Jitter(0.007));
  }

  private double Jitter(double max) => ((_random.NextDouble() * 2) - 1) * max;

  private T Pick<T>(IReadOnlyList<T> items) => items[_random.Next(items.Count)];

  private async Task AddPhotoAsync(Bike bike, PhotoKind kind, DateTimeOffset now, CancellationToken ct)
  {
    var look = new BikeLook(bike.Type, bike.ColorPrimary ?? "black", bike.ColorSecondary,
      bike.Features.Contains("basket"), bike.Features.Contains("rack"), bike.Features.Contains("fenders"), bike.IsEbike);
    if (!_imageCache.TryGetValue(look, out var jpeg))
    {
      jpeg = DemoImages.Bike(look, _imageCache.Count * 37);
      _imageCache[look] = jpeg;
    }

    await StorePhotoAsync(bike, kind, jpeg, now, ct);
  }

  private Task AddReceiptAsync(Bike bike, DateTimeOffset now, CancellationToken ct) =>
    StorePhotoAsync(bike, PhotoKind.Receipt,
      DemoImages.Receipt(bike.Brand, bike.Model, bike.FrameNoRaw, bike.PurchaseDate ?? DateOnly.FromDateTime(now.UtcDateTime), "1.249,00 €"),
      now, ct);

  /// <summary>Same pipeline as an upload (re-encode, max 1600 px, thumbnail).</summary>
  private async Task StorePhotoAsync(Bike bike, PhotoKind kind, byte[] jpeg, DateTimeOffset now, CancellationToken ct)
  {
    var processed = images.Process(new MemoryStream(jpeg));
    var photoId = Guid.CreateVersion7();
    var key = PhotoStorageKeys.Full(bike.Id, photoId);
    _storedKeys.Add(key);
    _storedKeys.Add(PhotoStorageKeys.Thumbnail(key));
    await storage.SaveAsync(key, new MemoryStream(processed.Full), ct);
    await storage.SaveAsync(PhotoStorageKeys.Thumbnail(key), new MemoryStream(processed.Thumbnail), ct);
    bike.Photos.Add(new BikePhoto(photoId, bike.Id, kind, key, now));
  }

  /// <summary>
  /// Files to upload by hand in the demo (verification flow E). The fake vision extractor passes a check when the
  /// file name contains "pass". They show the clean demo bike (<see cref="DemoScenario.CleanFrame"/>).
  /// </summary>
  private static async Task<IReadOnlyList<string>> WriteSamplePhotosAsync(string directory, CancellationToken ct)
  {
    Directory.CreateDirectory(directory);
    var purchase = new DateOnly(2026, 6, 12);
    var files = new Dictionary<string, byte[]>
    {
      ["receipt_pass.jpg"] = DemoImages.Receipt("Stevens", "Courier Luxe", DemoScenario.CleanFrame, purchase, "849,00 €"),
      ["receipt_fail.jpg"] = DemoImages.Receipt(null, null, null, purchase, "17,50 €"),
      ["possession_pass.jpg"] = DemoImages.FrameNumberCloseUp(DemoScenario.CleanFrame, "Code: ____"),
      ["possession_fail.jpg"] = DemoImages.FrameNumberCloseUp("WGA 1234 5678", "Code: ____"),
      ["bike_side.jpg"] = DemoImages.Bike(new BikeLook(BikeType.City, "green", null, false, true, true, false), 120),
    };

    foreach (var (name, bytes) in files)
    {
      await File.WriteAllBytesAsync(Path.Combine(directory, name), bytes, ct);
    }

    return files.Keys.ToList();
  }

  private static FeinCode ParseFein(string value) =>
    FeinCode.TryCreate(value, out var code) ? code : throw new InvalidOperationException($"Invalid demo FEIN code {value}.");

  private static void Check(IdentityResult result, string what)
  {
    if (!result.Succeeded)
    {
      throw new InvalidOperationException($"Seeding {what} failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
  }
}

public sealed record DemoSeedResult(
  int Users,
  int Bikes,
  int OpenTheftReports,
  int TheftReports,
  int Photos,
  IReadOnlyDictionary<TrustLevel, int> TrustLevels,
  DateTimeOffset TransferExpiresAt,
  IReadOnlyList<string> SamplePhotos);
