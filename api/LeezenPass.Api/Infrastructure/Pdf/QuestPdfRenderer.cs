using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LeezenPass.Api.Infrastructure.Pdf;

/// <summary>QuestPDF runs fully offline, so there is no Fake. Licence is set in Program.cs.</summary>
public class QuestPdfRenderer : IPdfRenderer
{
  public byte[] Render(PdfDocumentModel document) =>
    Document.Create(container =>
    {
      container.Page(page =>
      {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.DefaultTextStyle(x => x.FontSize(11));

        page.Header().Column(col =>
        {
          col.Item().Text(document.Title).FontSize(22).Bold();
          col.Item().Text(document.Subtitle).FontSize(12).FontColor(Colors.Grey.Darken1);
        });

        page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
        {
          table.ColumnsDefinition(c =>
          {
            c.ConstantColumn(5, Unit.Centimetre);
            c.RelativeColumn();
          });

          foreach (var field in document.Fields)
          {
            table.Cell().PaddingVertical(4).Text(field.Label).SemiBold();
            table.Cell().PaddingVertical(4).Text(field.Value);
          }
        });

        if (document.Footer is not null)
        {
          page.Footer().Text(document.Footer).FontSize(9).FontColor(Colors.Grey.Darken1);
        }
      });
    }).GeneratePdf();
}
