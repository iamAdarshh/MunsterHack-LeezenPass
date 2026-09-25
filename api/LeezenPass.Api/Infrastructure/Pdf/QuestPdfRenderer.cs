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
          col.Item().Text(document.Title).FontSize(22).Bold().FontColor("#0f766e");
          col.Item().Text(document.Subtitle).FontSize(12).FontColor(Colors.Grey.Darken1);
        });

        page.Content().PaddingVertical(0.8f, Unit.Centimetre).Column(col =>
        {
          col.Spacing(12);

          if (document.Image is not null)
          {
            col.Item().MaxHeight(7, Unit.Centimetre).AlignCenter().Image(document.Image).FitArea();
          }

          col.Item().Table(table =>
          {
            table.ColumnsDefinition(c =>
            {
              c.ConstantColumn(5.5f, Unit.Centimetre);
              c.RelativeColumn();
            });

            foreach (var field in document.Fields)
            {
              table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).Text(field.Label).SemiBold();
              table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).Text(field.Value);
            }
          });

          if (document.QrCodeText is not null)
          {
            col.Item().PaddingTop(8).Row(row =>
            {
              row.ConstantItem(3.5f, Unit.Centimetre).Image(QrCodes.Png(document.QrCodeText));
              row.RelativeItem().PaddingLeft(12).AlignMiddle().Column(text =>
              {
                if (document.QrCaption is not null)
                {
                  text.Item().Text(document.QrCaption);
                }

                text.Item().Text(document.QrCodeText).FontSize(9).FontColor(Colors.Grey.Darken1);
              });
            });
          }
        });

        page.Footer().Row(row =>
        {
          row.RelativeItem().Text(document.Footer ?? string.Empty).FontSize(9).FontColor(Colors.Grey.Darken1);
          row.ConstantItem(2, Unit.Centimetre).AlignRight().Text(x =>
          {
            x.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Darken1));
            x.CurrentPageNumber();
            x.Span(" / ");
            x.TotalPages();
          });
        });
      });
    }).GeneratePdf();
}
