using System.Globalization;
using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Features.Bikes.ExportPass;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Pdf;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Transfers.Certificate;

public sealed record CertificateRequest
{
  public Guid Id { get; init; }
}

/// <summary>Certificate of the ownership transfer for the new owner. QR code -> public /verify/{token}.</summary>
public class CertificateEndpoint(
  AppDbContext db,
  IPdfRenderer pdf,
  IClock clock,
  IOptions<AppOptions> app,
  IOptions<FeaturesOptions> features) : Endpoint<CertificateRequest>
{
  private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
  private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

  private static string LocalTime(DateTimeOffset t) => TimeZoneInfo.ConvertTime(t, Berlin).ToString("g", German);

  public override void Configure()
  {
    Get("transfers/{id}/certificate.pdf");
    Summary(s => s.Summary = "Transfer certificate PDF (new owner only)");
  }

  public override async Task HandleAsync(CertificateRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var data = await db.OwnershipTransfers
      .Where(t => t.Id == req.Id && t.ToUserId == userId && t.CompletedAt != null && t.VerifyToken != null)
      .Join(db.Bikes, t => t.BikeId, b => b.Id, (t, b) => new { Transfer = t, Bike = b })
      .FirstOrDefaultAsync(ct);
    if (data is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var (transfer, bike) = (data.Transfer, data.Bike);
    var email = await db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstAsync(ct);
    var verifyUrl = $"{app.Value.PublicBaseUrl.TrimEnd('/')}/verify/{transfer.VerifyToken}";

    var fields = new List<PdfField>
    {
      new("Rahmennummer / Frame no.", bike.FrameNoRaw),
      new("Marke, Modell / Make, model", string.Join(" ", new[] { bike.Brand, bike.Model }.OfType<string>()) is { Length: > 0 } name ? name : "–"),
      new("Typ / Type", PassText.Type(bike.Type)),
      new("Farben / Colours", PassText.Colors(bike.ColorPrimary, bike.ColorSecondary)),
      new("Übertragen am / Transferred", LocalTime(transfer.CompletedAt!.Value)),
      // Seller identity is not printed (data minimisation); the platform vouches for the handover.
      new("Vorbesitz / Previous owner", "verifiziertes LeezenPass-Konto / verified account"),
      new("Neues Eigentümer-Konto / New owner", email ?? "–"),
      new("Nachweis-Nr. / Certificate no.", transfer.Id.ToString("N")[^12..].ToUpperInvariant()),
    };

    var document = new PdfDocumentModel(
      Title: "Eigentumsnachweis",
      Subtitle: "Certificate of ownership transfer · LeezenPass Münster",
      Fields: fields,
      Footer: $"Erstellt am {LocalTime(clock.UtcNow)} mit LeezenPass" + (features.Value.DemoMode ? " · Demo-Daten" : string.Empty),
      QrCodeText: verifyUrl,
      QrCaption: "Echtheit prüfen: QR-Code scannen. / Scan to verify this certificate.");

    await Send.BytesAsync(pdf.Render(document), $"eigentumsnachweis-{bike.FrameNoNorm}.pdf", "application/pdf", cancellation: ct);
  }
}
