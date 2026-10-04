using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace planno_API.Services
{
    public record InvoiceData(
    string InvoiceNumber, DateTime IssuedAt, string CustomerName, string CustomerEmail,
    string Description, DateTime PeriodStart, DateTime PeriodEnd,
    decimal Gross, string CardBrand, string CardLast4);

    public interface IInvoicePdfGenerator
    {
        byte[] Generate(InvoiceData data);
    }

    public class InvoicePdfGenerator : IInvoicePdfGenerator
    {
        private const decimal VatRate = 0.20m;
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("de-AT");

        public byte[] Generate(InvoiceData d)
        {
            decimal net = Math.Round(d.Gross / (1 + VatRate), 2);
            decimal vat = d.Gross - net;

            return Document.Create(container => container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(t => t.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("Invoice").FontSize(26).Bold();
                    col.Item().Text($"Invoice No. {d.InvoiceNumber}").FontSize(12);
                });

                page.Content().PaddingVertical(25).Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Text($"{d.CustomerName}\n{d.CustomerEmail}");
                    col.Item().Text($"Invoice Date: {d.IssuedAt:dd.MM.yyyy}");
                    col.Item().Text($"Billing Period: {d.PeriodStart:dd.MM.yyyy} – {d.PeriodEnd:dd.MM.yyyy}");

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(110); });

                        table.Cell().Text(d.Description).Bold();
                        table.Cell().AlignRight().Text(net.ToString("C", Culture));

                        table.Cell().Text("VAT 20%");
                        table.Cell().AlignRight().Text(vat.ToString("C", Culture));

                        table.Cell().Text("Total (Gross)").Bold();
                        table.Cell().AlignRight().Text(d.Gross.ToString("C", Culture)).Bold();
                    });

                    col.Item().Text($"Paid with {d.CardBrand} •••• {d.CardLast4}");
                });

                page.Footer().Text("Demo Invoice: No actual payment was processed.").FontSize(9).FontColor(Colors.Grey.Darken1);
            })).GeneratePdf();
        }
    }
}
