using MoneyTracker.Application.DTOs.Transactions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MoneyTracker.UI.Services.Print;

public class TransactionPrintService : ITransactionPrintService
{
    public byte[] GenerateReport(IList<TransactionDto> transactions, string title)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var expense = transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
        var net = transactions.Sum(t => t.Amount);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(16).Bold().AlignCenter();
                    col.Item().Text($"Generated: {DateTime.Today:MMMM d, yyyy}")
                        .FontSize(8).AlignCenter();
                    col.Item().PaddingBottom(8);
                });

                page.Content().Column(col =>
                {
                    // Transaction table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(64);  // Date
                            cols.RelativeColumn(3);   // Name
                            cols.RelativeColumn(2);   // Account
                            cols.RelativeColumn(2);   // Category
                            cols.ConstantColumn(72);  // Amount
                        });

                        // Header
                        table.Header(header =>
                        {
                            void Cell(string text) =>
                                header.Cell().Background("#374151").Padding(4)
                                    .Text(text).FontColor("#FFFFFF").Bold();

                            Cell("Date");
                            Cell("Name");
                            Cell("Account");
                            Cell("Category");
                            header.Cell().Background("#374151").Padding(4).AlignRight()
                                .Text("Amount").FontColor("#FFFFFF").Bold();
                        });

                        // Rows
                        foreach (var (t, i) in transactions.Select((t, i) => (t, i)))
                        {
                            var bg = i % 2 == 0 ? "#FFFFFF" : "#F9FAFB";
                            var amountColor = t.Amount >= 0 ? "#059669" : "#DC2626";

                            table.Cell().Background(bg).Padding(4)
                                .Text(t.Date.ToString("MM/dd/yy"));
                            table.Cell().Background(bg).Padding(4)
                                .Text(t.Name);
                            table.Cell().Background(bg).Padding(4)
                                .Text(t.Account?.Name ?? "");
                            table.Cell().Background(bg).Padding(4)
                                .Text(t.Category?.Name ?? "");
                            table.Cell().Background(bg).Padding(4).AlignRight()
                                .Text(FormatAmount(t.Amount)).FontColor(amountColor);
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.ConstantColumn(100);
                        });

                        void TotalRow(string label, decimal value, string color)
                        {
                            table.Cell().AlignRight().Padding(3).Text(label).Bold();
                            table.Cell().AlignRight().Padding(3)
                                .Text(FormatAmount(value)).FontColor(color).Bold();
                        }

                        TotalRow("Total Income:", income, "#059669");
                        TotalRow("Total Expense:", expense, "#DC2626");
                        TotalRow("Net:", net, net >= 0 ? "#059669" : "#DC2626");
                        table.Cell().ColumnSpan(2).BorderTop(1).BorderColor("#D1D5DB").Height(1);
                        table.Cell().AlignRight().Padding(3).Text($"{transactions.Count} transactions").FontColor("#6B7280");
                        table.Cell();
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontSize(8).FontColor("#9CA3AF");
                    x.CurrentPageNumber().FontSize(8).FontColor("#9CA3AF");
                    x.Span(" of ").FontSize(8).FontColor("#9CA3AF");
                    x.TotalPages().FontSize(8).FontColor("#9CA3AF");
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static string FormatAmount(decimal amount)
        => amount >= 0 ? $"${amount:N2}" : $"-${Math.Abs(amount):N2}";
}
