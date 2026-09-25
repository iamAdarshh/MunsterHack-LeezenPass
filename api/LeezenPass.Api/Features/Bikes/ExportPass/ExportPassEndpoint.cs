using System.Globalization;
using FastEndpoints;
using LeezenPass.Api.Configurations;
using LeezenPass.Api.Domain.Bikes;
using LeezenPass.Api.Infrastructure.Data;
using LeezenPass.Api.Infrastructure.Identity;
using LeezenPass.Api.Infrastructure.Pdf;
using LeezenPass.Api.Infrastructure.Storage;
using LeezenPass.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeezenPass.Api.Features.Bikes.ExportPass;

public sealed record ExportPassRequest
{
  public Guid Id { get; init; }
}

/// <summary>Bike pass PDF for police and insurance. German first, English subtitles. Owner only.</summary>
public class ExportPassEndpoint(
  AppDbContext db,
  IFileStorage storage,
  IPdfRenderer pdf,
  IClock clock,
  IOptions<AppOptions> app,
  IOptions<FeaturesOptions> features) : Endpoint<ExportPassRequest>
{
  private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
  private static readonly TimeZoneInfo Berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

  private static string LocalTime(DateTimeOffset t) => TimeZoneInfo.ConvertTime(t, Berlin).ToString("g", German);

  public override void Configure()
  {
    Get("bikes/{id}/pass.pdf");
    Summary(s => s.Summary = "Bike pass as PDF (with QR code to the public tag page)");
  }

  public override async Task HandleAsync(ExportPassRequest req, CancellationToken ct)
  {
    var userId = User.GetUserId();
    var bike = await db.Bikes.AsNoTracking().WithDetails()
      .FirstOrDefaultAsync(b => b.Id == req.Id && b.OwnerId == userId, ct);
    if (bike is null)
    {
      await Send.NotFoundAsync(ct);
      return;
    }

    var email = await db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstAsync(ct);
    var tagUrl = $"{app.Value.PublicBaseUrl.TrimEnd('/')}/b/{bike.PublicToken}";

    var fields = new List<PdfField>
    {
      new("Rahmennummer / Frame no.", bike.FrameNoRaw),
      new("Marke, Modell / Make, model", string.Join(" ", new[] { bike.Brand, bike.Model }.OfType<string>()) is { Length: > 0 } name ? name : "–"),
      new("Typ / Type", PassText.Type(bike.Type)),
      new("Farben / Colours", PassText.Colors(bike.ColorPrimary, bike.ColorSecondary)),
      new("E-Bike", bike.IsEbike ? $"Ja{(bike.BatterySerial is null ? "" : $", Akku-Nr. {bike.BatterySerial}")}" : "Nein"),
      new("Merkmale / Features", PassText.Features(bike.Features)),
      new("Kaufdatum / Purchased", bike.PurchaseDate?.ToString("d", German) ?? "–"),
      // The code itself is never stored in plain text, so the pass can only say that one is on file.
      new("FEIN-Code", bike.FeinCodeHash is null ? "–" : "hinterlegt (verschlüsselt) / on file"),
      new("Status", PassText.Status(bike.Status)),
      new("Registriert seit / Since", TimeZoneInfo.ConvertTime(bike.CreatedAt, Berlin).ToString("d", German)),
      new("Eigentümer-Konto / Owner account", email ?? "–"),
    };

    if (bike.OpenTheftReport is { } theft)
    {
      fields.Add(new("Gestohlen am / Stolen on", LocalTime(theft.StolenAt)));
      fields.Add(new("Aktenzeichen / Case no.", theft.PoliceCaseNo ?? "noch keins / none yet"));
    }

    var footer = $"Erstellt am {LocalTime(clock.UtcNow)} mit LeezenPass"
      + (features.Value.DemoMode ? " · Demo-Daten" : string.Empty);

    var document = new PdfDocumentModel(
      Title: "Fahrradpass",
      Subtitle: "Bike pass · LeezenPass Münster",
      Fields: fields,
      Footer: footer,
      Image: await SidePhoto(bike, ct),
      QrCodeText: tagUrl,
      QrCaption: "Fahrrad gefunden? QR-Code scannen. / Found this bike? Scan the QR code.");

    var bytes = pdf.Render(document);
    await Send.BytesAsync(bytes, $"fahrradpass-{bike.FrameNoNorm}.pdf", "application/pdf", cancellation: ct);
  }

  private async Task<byte[]?> SidePhoto(Bike bike, CancellationToken ct)
  {
    var photo = bike.Photos.Where(p => p.Kind == PhotoKind.Side).OrderBy(p => p.CreatedAt).FirstOrDefault();
    if (photo is null)
    {
      return null;
    }

    await using var stream = await storage.OpenReadAsync(photo.Path, ct);
    if (stream is null)
    {
      return null;
    }

    using var buffer = new MemoryStream();
    await stream.CopyToAsync(buffer, ct);
    return buffer.ToArray();
  }
}
