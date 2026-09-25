# API (ASP.NET Core 10)

Loaded when working in `api/`. Root rules in `/CLAUDE.md` still apply.

## Structure

```
api/LeezenPass.Api/
  Domain/            Entities + value objects (FrameNumber, FeinCode, TransferCode). No EF/ASP.NET refs.
  Features/
    Bikes/Register/  RegisterBikeEndpoint.cs, RegisterBikeRequest.cs, RegisterBikeResponse.cs, RegisterBikeValidator.cs
    Bikes/List/ Bikes/Get/ Bikes/Update/ Bikes/Delete/ Bikes/UploadPhoto/ Bikes/ExportPass/
    Check/
    Theft/Report/ Theft/MarkRecovered/ Stolen/List/
    Transfers/Create/ Transfers/Claim/ Transfers/Certificate/ Verify/
    Ai/Extract/
    Sightings/ RiskMap/ Tags/        (stretch)
  Infrastructure/
    Data/            AppDbContext, entity configurations, migrations
    Storage/         IFileStorage: LocalFileStorage, MinioFileStorage
    Vision/          IVisionExtractor: <Provider>VisionExtractor, FakeVisionExtractor
    Pdf/             IPdfRenderer: QuestPdfRenderer
    Email/           IEmailSender: SmtpEmailSender (Mailpit in dev)
    Captcha/         ICaptchaVerifier: TurnstileVerifier, FakeCaptchaVerifier
    Time/            IClock: SystemClock
    Images/          ImageProcessor (SkiaSharp): orientation, EXIF strip, resize, thumbnail
api/LeezenPass.Api.Tests/   xUnit tests for Domain + scoring
```

Adapt folder names to what the Ardalis Minimal Clean template generates; keep the idea (one folder per use case).

## Endpoints (FastEndpoints)

- One endpoint class per use case. Configure route, verb, auth and rate limiting in `Configure()`.
- Request/response are records. Validation via FluentValidation validator next to the request.
- Owner endpoints: load the bike with `Where(b => b.Id == id && b.OwnerId == userId)`; return 404 (not 403) if not found, so existence isn't leaked.
- Always pass `ct` (CancellationToken) to EF calls.
- Public list endpoints project to DTOs with `Select(...)`; never return entities.

## Auth

- `builder.Services.AddIdentityApiEndpoints<AppUser>()` + `app.MapGroup("/api/auth").MapIdentityApi<AppUser>()`.
- Login with `?useCookies=true`. Add a custom `POST /api/auth/logout` (SignInManager.SignOutAsync) — MapIdentityApi has none.
- Cookie: HttpOnly, Secure, SameSite=Lax; return 401 instead of redirecting to a login page for `/api/*`.
- CSRF: SameSite=Lax + JSON-only bodies; multipart upload endpoints require header `X-LeezenPass: 1`.
- `AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>`.

## Data

- Npgsql with `UseNetTopologySuite()`. Geography points SRID 4326 (lon, lat order in `new Point(lon, lat)`).
- Metric calculations (distance, hex grid) → `ST_Transform(..., 25832)` or `geography` distance functions. Never compute distances in degrees.
- Enums stored as strings.
- Migrations: one per slice, descriptive names. Never edit an applied migration.
- Enable PostGIS via `modelBuilder.HasPostgresExtension("postgis")`.

## Rate limiting

- Built-in `AddRateLimiter`: policy `check` (10 per 10 min per IP), `relay` (5 per hour per IP), `upload` (30 per hour per user).
- IP for logging = SHA-256(ip + daily salt).

## PDFs

- At startup: `QuestPDF.Settings.License = LicenseType.Community;`
- QR images via QRCoder (PNG bytes) embedded in QuestPDF.
- Bike pass and certificate are German-first with English subtitle.

## Config

- Options classes bound from config: `Storage`, `Vision`, `Email`, `Captcha`, `Fein` (HmacSecret), `Features` (UseFakes).
- `Features:UseFakes=true` switches every external dependency to its Fake.
- Secrets from environment / user-secrets. Document every key in `infra/.env.example`.
