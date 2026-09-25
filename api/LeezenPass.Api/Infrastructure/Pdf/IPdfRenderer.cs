namespace LeezenPass.Api.Infrastructure.Pdf;

/// <summary>Renders simple document PDFs (bike pass, transfer certificate).</summary>
public interface IPdfRenderer
{
  byte[] Render(PdfDocumentModel document);
}

/// <param name="Title">Big heading (German).</param>
/// <param name="Subtitle">Line under the title (English subtitle, context).</param>
/// <param name="Fields">Label/value table.</param>
/// <param name="Footer">Small print at the bottom.</param>
/// <param name="Image">Optional JPEG/PNG shown above the table (e.g. side photo).</param>
/// <param name="QrCodeText">Optional URL rendered as QR code.</param>
/// <param name="QrCaption">Text next to the QR code.</param>
public sealed record PdfDocumentModel(
  string Title,
  string Subtitle,
  IReadOnlyList<PdfField> Fields,
  string? Footer = null,
  byte[]? Image = null,
  string? QrCodeText = null,
  string? QrCaption = null);

/// <param name="Label">German label, optionally with " / English".</param>
/// <param name="Value">Value text.</param>
public sealed record PdfField(string Label, string Value);
