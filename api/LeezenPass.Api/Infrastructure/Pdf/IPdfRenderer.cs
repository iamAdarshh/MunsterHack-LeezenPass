namespace LeezenPass.Api.Infrastructure.Pdf;

/// <summary>Renders simple document PDFs (bike pass, transfer certificate).</summary>
public interface IPdfRenderer
{
  byte[] Render(PdfDocumentModel document);
}

public sealed record PdfDocumentModel(
  string Title,
  string Subtitle,
  IReadOnlyList<PdfField> Fields,
  string? Footer = null);

public sealed record PdfField(string Label, string Value);
