using MoneyTracker.Application.DTOs.Loans;
using MoneyTracker.Domain.Enums.Loans;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MoneyTracker.UI.Services.Print;

public class LoanPrintService : ILoanPrintService
{
    private const string Navy     = "#0F172A";
    private const string NavyLight = "#1E293B";
    private const string White    = "#FFFFFF";
    private const string Gray50   = "#F8FAFC";
    private const string Gray200  = "#E2E8F0";
    private const string Gray400  = "#94A3B8";
    private const string Gray600  = "#475569";
    private const string Teal     = "#0D9488";
    private const string TealBg   = "#CCFBF1";
    private const string Teal400  = "#2DD4BF";
    private const string Lime     = "#65A30D";
    private const string LimeBg   = "#ECFCCB";
    private const string Lime400  = "#A3E635";
    private const string Green    = "#10B981";
    private const string GreenDark = "#059669";
    private const string Red      = "#DC2626";
    private const string RedBg    = "#FEE2E2";
    private const string Orange   = "#F59E0B";
    private const string Blue     = "#3B82F6";

    public byte[] GenerateReport(IList<LoanDto> loans)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(16);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));
                page.Background(Gray50);

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(8).AlignCenter()
                        .Text("LOAN PAYMENT HISTORY")
                        .FontSize(7).FontColor(Gray400).Bold().LetterSpacing(1);

                    foreach (var (loan, idx) in loans.Select((l, i) => (l, i)))
                    {
                        if (idx > 0)
                            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Gray200);
                        RenderTicket(col, loan);
                    }

                    if (loans.Count > 1)
                    {
                        col.Item().PaddingTop(10).LineHorizontal(2).LineColor(Navy);
                        col.Item().PaddingTop(6).Background(Navy).Padding(10).Column(summary =>
                        {
                            summary.Item().AlignCenter()
                                .Text("COMBINED SUMMARY")
                                .FontSize(7).FontColor(Gray400).Bold().LetterSpacing(1);
                            summary.Item().PaddingTop(6).Row(row =>
                            {
                                SummaryCell(row, "Total Lent",    Fmt(loans.Sum(l => l.PrincipalAmount)));
                                SummaryCell(row, "Total Paid",    Fmt(loans.Sum(l => l.TotalPaid)));
                                SummaryCell(row, "Outstanding",   Fmt(loans.Sum(l => l.Balance)));
                            });
                        });
                    }

                    col.Item().PaddingTop(10).AlignCenter()
                        .Text($"Generated on {DateTime.Today:MMM d, yyyy}")
                        .FontSize(7).FontColor(Gray400);
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void RenderTicket(ColumnDescriptor col, LoanDto loan)
    {
        var (badgeText, badgeBg, badgeFg, barColor) = loan.Status switch
        {
            LoanStatus.PaidOff  => ("PAID OFF",  LimeBg,  Lime,   Lime400),
            LoanStatus.Forgiven => ("FORGIVEN",  Gray200, Gray600, Gray400),
            _ when loan.IsOverdue => ("OVERDUE", RedBg,   Red,    Red),
            _                   => ("ACTIVE",    TealBg,  Teal,   Teal400)
        };

        // ── Header ────────────────────────────────────────────────
        col.Item().Background(Navy).Padding(12).Column(header =>
        {
            header.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(loan.ContactName)
                        .FontSize(13).Bold().FontColor(White);
                    if (!string.IsNullOrWhiteSpace(loan.Description))
                        c.Item().PaddingTop(1).Text(loan.Description)
                            .FontSize(8).FontColor(Gray400);
                    if (loan.DueDate.HasValue)
                        c.Item().PaddingTop(3).Text($"Due: {loan.DueDate.Value:MMM d, yyyy}")
                            .FontSize(7).FontColor(Gray400);
                });
                row.AutoItem().AlignMiddle()
                    .Background(badgeBg).Padding(5)
                    .Text(badgeText)
                    .FontSize(7).Bold().FontColor(badgeFg);
            });

            // Progress bar + installment count
            if (loan.TotalOwed > 0)
            {
                var pct       = (double)Math.Min(100, loan.ProgressPercent);
                var instPaid  = loan.Installments.Count(i => i.IsPaid);
                var instTotal = loan.Installments.Count;

                header.Item().PaddingTop(10).Column(pb =>
                {
                    pb.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"{pct}% paid")
                            .FontSize(7).FontColor(Gray400);
                        if (instTotal > 0)
                            r.AutoItem().Text($"{instPaid}/{instTotal} cuotas")
                                .FontSize(7).FontColor(Gray400);
                    });
                    pb.Item().PaddingTop(3).Height(5).Row(bar =>
                    {
                        if (pct > 0)
                            bar.RelativeItem((float)pct).Background(barColor).Height(5);
                        if (pct < 100)
                            bar.RelativeItem((float)(100 - pct)).Background(NavyLight).Height(5);
                    });
                });
            }
        });

        // ── Three metrics: Principal | Paid | Outstanding ─────────
        col.Item().Background(White).Padding(10).Row(row =>
        {
            MetricCell(row, "Principal",   Fmt(loan.PrincipalAmount), Blue);
            row.ConstantItem(1).Background(Gray200);
            MetricCell(row, "Total Paid",  Fmt(loan.TotalPaid),       GreenDark);
            row.ConstantItem(1).Background(Gray200);
            MetricCell(row, "Outstanding", Fmt(loan.Balance),         loan.Balance > 0 ? Red : GreenDark);
        });

        // ── Visual installment grid ───────────────────────────────
        if (loan.Installments.Any())
        {
            col.Item().PaddingTop(1).Background(White).Padding(10).Column(grid =>
            {
                grid.Item().PaddingBottom(5)
                    .Text("INSTALLMENT GRID")
                    .FontSize(7).Bold().FontColor(Gray400).LetterSpacing(1);

                const int perRow = 12;
                var sorted = loan.Installments.OrderBy(i => i.InstallmentNumber).ToList();
                var chunks = sorted
                    .Select((inst, idx) => (inst, idx))
                    .GroupBy(x => x.idx / perRow);

                foreach (var chunk in chunks)
                {
                    grid.Item().PaddingBottom(2).Row(row =>
                    {
                        foreach (var (inst, _) in chunk)
                        {
                            var cellBg = inst.IsPaid          ? Teal400
                                       : inst.IsOverdue       ? Red
                                       : inst.IsPartiallyPaid ? Orange
                                       : Gray200;
                            var fg = (inst.IsPaid || inst.IsOverdue) ? White : Gray600;

                            row.ConstantItem(16).Height(16).Padding(1)
                                .Background(cellBg).AlignCenter().AlignMiddle()
                                .Text($"{inst.InstallmentNumber}")
                                .FontSize(6).FontColor(fg);
                        }
                        // pad last row
                        var remainder = perRow - chunk.Count();
                        for (var i = 0; i < remainder; i++)
                            row.ConstantItem(16).Height(16);
                    });
                }

                grid.Item().PaddingTop(4).Row(legend =>
                {
                    LegendDot(legend, Teal400, "Paid");
                    LegendDot(legend, Orange,  "Partial");
                    LegendDot(legend, Red,     "Overdue");
                    LegendDot(legend, Gray200, "Pending");
                });
            });
        }

        // ── Installment plan table ────────────────────────────────
        if (loan.Installments.Any())
        {
            col.Item().PaddingTop(1).Background(White).Padding(10).Column(cuotas =>
            {
                cuotas.Item().PaddingBottom(4)
                    .Text("INSTALLMENT PLAN")
                    .FontSize(7).Bold().FontColor(Gray400).LetterSpacing(1);

                cuotas.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(14);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().PaddingVertical(2).Text("#").FontSize(6.5f).FontColor(Gray400).Bold();
                        h.Cell().PaddingVertical(2).Text("Due Date").FontSize(6.5f).FontColor(Gray400).Bold();
                        h.Cell().PaddingVertical(2).AlignRight().Text("Expected").FontSize(6.5f).FontColor(Gray400).Bold();
                        h.Cell().PaddingVertical(2).AlignRight().Text("Paid").FontSize(6.5f).FontColor(Gray400).Bold();
                        h.Cell().PaddingVertical(2).AlignRight().Text("Pending").FontSize(6.5f).FontColor(Gray400).Bold();
                    });

                    foreach (var (inst, i) in loan.Installments
                        .OrderBy(x => x.InstallmentNumber)
                        .Select((x, i) => (x, i)))
                    {
                        var bg           = i % 2 == 0 ? White : Gray50;
                        var pendingColor = inst.IsOverdue ? Red : inst.PendingAmount > 0 ? Orange : Gray400;
                        var paidColor    = inst.PaidAmount > 0 ? GreenDark : Gray400;

                        table.Cell().Background(bg).PaddingVertical(2)
                            .Text($"{inst.InstallmentNumber}").FontSize(7).FontColor(Gray600);
                        table.Cell().Background(bg).PaddingVertical(2)
                            .Text(inst.DueDate.ToString("MM/dd/yy")).FontSize(7);
                        table.Cell().Background(bg).PaddingVertical(2).AlignRight()
                            .Text(Fmt(inst.ExpectedAmount)).FontSize(7);
                        table.Cell().Background(bg).PaddingVertical(2).AlignRight()
                            .Text(inst.PaidAmount > 0 ? Fmt(inst.PaidAmount) : "$0.00")
                            .FontSize(7).FontColor(paidColor);
                        table.Cell().Background(bg).PaddingVertical(2).AlignRight()
                            .Text(inst.PendingAmount > 0 ? Fmt(inst.PendingAmount) : "—")
                            .FontSize(7).FontColor(pendingColor);
                    }
                });
            });
        }

        // ── Payment history ───────────────────────────────────────
        col.Item().PaddingTop(1).Background(White).Padding(10).Column(hist =>
        {
            hist.Item().PaddingBottom(4)
                .Text("PAYMENT HISTORY")
                .FontSize(7).Bold().FontColor(Gray400).LetterSpacing(1);

            if (!loan.Payments.Any())
            {
                hist.Item().AlignCenter()
                    .Text("No payments recorded.")
                    .FontSize(8).Italic().FontColor(Gray400);
            }
            else
            {
                var sorted = loan.Payments.OrderBy(p => p.Date).ToList();
                foreach (var (p, i) in sorted.Select((x, i) => (x, i)))
                {
                    var bg = i % 2 == 0 ? White : Gray50;
                    hist.Item().Background(bg).Padding(4).Row(row =>
                    {
                        row.ConstantItem(16).AlignMiddle()
                            .Text($"{i + 1}").FontSize(7).FontColor(Gray400);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(p.Date.ToString("MMM d, yyyy"))
                                .FontSize(8).Bold().FontColor(Gray600);
                            if (!string.IsNullOrWhiteSpace(p.Notes))
                                c.Item().Text(p.Notes).FontSize(7).Italic().FontColor(Gray400);
                        });
                        row.ConstantItem(64).AlignRight().AlignMiddle()
                            .Text(Fmt(p.Amount)).FontSize(9).Bold().FontColor(GreenDark);
                    });

                    if (i < sorted.Count - 1)
                        hist.Item().LineHorizontal(0.5f).LineColor(Gray200);
                }

                hist.Item().PaddingTop(6).LineHorizontal(1).LineColor(Gray200);
                hist.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text("Total Paid").FontSize(8).Bold().FontColor(Navy);
                    row.AutoItem().Text(Fmt(loan.TotalPaid)).FontSize(10).Bold().FontColor(GreenDark);
                });
            }
        });
    }

    private static void MetricCell(RowDescriptor row, string label, string value, string valueColor)
    {
        row.RelativeItem().PaddingVertical(4).AlignCenter().Column(c =>
        {
            c.Item().AlignCenter().Text(label).FontSize(7).FontColor(Gray400);
            c.Item().AlignCenter().PaddingTop(2).Text(value).FontSize(10).Bold().FontColor(valueColor);
        });
    }

    private static void LegendDot(RowDescriptor row, string color, string label)
    {
        row.AutoItem().PaddingRight(8).Row(r =>
        {
            r.ConstantItem(8).Height(8).AlignMiddle().Background(color);
            r.AutoItem().PaddingLeft(3).AlignMiddle().Text(label).FontSize(6).FontColor(Gray600);
        });
    }

    private static void SummaryCell(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().AlignCenter().Column(c =>
        {
            c.Item().AlignCenter().Text(label).FontSize(7).FontColor(Gray400);
            c.Item().AlignCenter().Text(value).FontSize(10).Bold().FontColor(White);
        });
    }

    private static string Fmt(decimal amount) => $"${amount:N2}";
}
