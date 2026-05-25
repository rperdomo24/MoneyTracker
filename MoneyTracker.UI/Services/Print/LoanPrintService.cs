using MoneyTracker.Application.DTOs.Loans;
using MoneyTracker.Domain.Enums.Loans;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MoneyTracker.UI.Services.Print;

public class LoanPrintService : ILoanPrintService
{
    private const string Navy = "#0F172A";
    private const string NavyLight = "#1E293B";
    private const string White = "#FFFFFF";
    private const string Gray50 = "#F8FAFC";
    private const string Gray200 = "#E2E8F0";
    private const string Gray400 = "#94A3B8";
    private const string Gray600 = "#475569";
    private const string Green = "#10B981";
    private const string GreenDark = "#059669";
    private const string Red = "#EF4444";
    private const string Orange = "#F59E0B";
    private const string Blue = "#3B82F6";

    public byte[] GenerateReport(IList<LoanDto> loans)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var contact = loans.FirstOrDefault()?.ContactName ?? "—";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Narrow ticket feel: A5 width, enough height
                page.Size(PageSizes.A5);
                page.Margin(16);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));
                page.Background(Gray50);

                page.Content().Column(col =>
                {
                    // Document title (small, top)
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
                                SummaryCell(row, "Total Lent", Fmt(loans.Sum(l => l.PrincipalAmount)));
                                SummaryCell(row, "Total Paid", Fmt(loans.Sum(l => l.TotalPaid)));
                                SummaryCell(row, "Outstanding Balance", Fmt(loans.Sum(l => l.Balance)));
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
        var statusColor = loan.Status switch
        {
            LoanStatus.PaidOff => Green,
            LoanStatus.Forgiven => Gray400,
            _ when loan.IsOverdue => Red,
            _ => Blue
        };
        var statusLabel = loan.Status switch
        {
            LoanStatus.PaidOff => "PAID OFF",
            LoanStatus.Forgiven => "FORGIVEN",
            _ when loan.IsOverdue => "OVERDUE",
            _ => "ACTIVE"
        };

        // ── Header block ──────────────────────────────────────────
        col.Item().Background(Navy).Padding(12).Column(header =>
        {
            // Contact + status
            header.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(loan.ContactName)
                        .FontSize(13).Bold().FontColor(White);
                    if (!string.IsNullOrWhiteSpace(loan.Description))
                        c.Item().Text(loan.Description)
                            .FontSize(8).FontColor(Gray400);
                });
                row.AutoItem().AlignMiddle()
                    .Text(statusLabel)
                    .FontSize(7).Bold().FontColor(statusColor);
            });

            // Big balance amount
            header.Item().PaddingTop(10).AlignCenter().Column(c =>
            {
                if (loan.Status == LoanStatus.Active && loan.Balance > 0)
                {
                    c.Item().Text("Outstanding Balance")
                        .FontSize(7).FontColor(Gray400).AlignCenter();
                    c.Item().Text(Fmt(loan.Balance))
                        .FontSize(22).Bold().FontColor(White).AlignCenter();
                }
                else if (loan.Status == LoanStatus.PaidOff)
                {
                    c.Item().Text("✓ Fully Paid Off")
                        .FontSize(10).Bold().FontColor(Green).AlignCenter();
                }
                else if (loan.OverpaymentAmount > 0)
                {
                    c.Item().Text("Credit Balance")
                        .FontSize(7).FontColor(Orange).AlignCenter();
                    c.Item().Text(Fmt(loan.OverpaymentAmount))
                        .FontSize(22).Bold().FontColor(Orange).AlignCenter();
                }
            });

            // Progress bar (manual via nested row)
            if (loan.TotalOwed > 0)
            {
                var pct = (double)Math.Min(100, loan.ProgressPercent);
                header.Item().PaddingTop(8).Column(pb =>
                {
                    pb.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"{pct}% paid")
                            .FontSize(7).FontColor(Gray400);
                        r.AutoItem().Text($"{Fmt(loan.TotalPaid)} / {Fmt(loan.TotalOwed)}")
                            .FontSize(7).FontColor(Gray400);
                    });
                    pb.Item().PaddingTop(3).Height(4).Row(bar =>
                    {
                        if (pct > 0)
                            bar.RelativeItem((float)pct).Background(Green).Height(4);
                        if (pct < 100)
                            bar.RelativeItem((float)(100 - pct)).Background(NavyLight).Height(4);
                    });
                });
            }
        });

        // ── Loan details ──────────────────────────────────────────
        col.Item().Background(White).Padding(10).Column(details =>
        {
            DetailRow(details, "Principal", Fmt(loan.PrincipalAmount));
            if (loan.InterestRate > 0)
            {
                DetailRow(details, $"Interest ({loan.InterestRate:0.##}%)", Fmt(loan.InterestAmount));
                DetailRow(details, "Loan Total", Fmt(loan.TotalOwed), bold: true);
            }
            if (loan.DueDate.HasValue)
                DetailRow(details, "Due Date", loan.DueDate.Value.ToString("dd/MM/yyyy"));
            if (loan.NumberOfInstallments.HasValue && loan.NumberOfInstallments > 0)
            {
                var paid = loan.Installments.Count(i => i.IsPaid);
                DetailRow(details, "Installments", $"{paid} / {loan.Installments.Count} paid");
            }
        });

        // ── Installment plan (if applicable) ─────────────────────
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
                        cols.ConstantColumn(14);   // #
                        cols.RelativeColumn(2);    // Due
                        cols.RelativeColumn(2);    // Expected
                        cols.RelativeColumn(2);    // Paid
                        cols.RelativeColumn(2);    // Pending
                    });

                    // mini header
                    table.Header(h =>
                    {
                        h.Cell().PaddingVertical(2).Text("#").FontSize(6.5f).FontColor(Gray400).Bold();
                        h.Cell().PaddingVertical(2).Text("Due Date").FontSize(6.5f).FontColor(Gray400).Bold();
                        h.Cell().PaddingVertical(2).AlignRight().Text("Expected").FontSize(6.5f).FontColor(Gray400).Bold();
                        h.Cell().PaddingVertical(2).AlignRight().Text("Paid").FontSize(6.5f).FontColor(Gray400).Bold();
                        h.Cell().PaddingVertical(2).AlignRight().Text("Pending").FontSize(6.5f).FontColor(Gray400).Bold();
                    });

                    foreach (var (inst, i) in loan.Installments.Select((x, i) => (x, i)))
                    {
                        var bg = i % 2 == 0 ? White : Gray50;
                        var pendingColor = inst.IsOverdue ? Red : inst.PendingAmount > 0 ? Orange : Gray400;
                        var paidColor = inst.PaidAmount > 0 ? GreenDark : Gray400;

                        table.Cell().Background(bg).PaddingVertical(2)
                            .Text($"{inst.InstallmentNumber}").FontSize(7).FontColor(Gray600);
                        table.Cell().Background(bg).PaddingVertical(2)
                            .Text(inst.DueDate.ToString("dd/MM/yy")).FontSize(7);
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
                            c.Item().Text(p.Date.ToString("dd/MM/yyyy"))
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

                // Subtotal
                hist.Item().PaddingTop(6).LineHorizontal(1).LineColor(Gray200);
                hist.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text("Total Paid").FontSize(8).Bold().FontColor(Navy);
                    row.AutoItem().Text(Fmt(loan.TotalPaid)).FontSize(10).Bold().FontColor(GreenDark);
                });
            }
        });
    }

    private static void DetailRow(ColumnDescriptor col, string label, string value, bool bold = false)
    {
        col.Item().PaddingVertical(2).Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(8).FontColor(Gray600);
            if (bold)
                row.AutoItem().Text(value).FontSize(8).Bold().FontColor(Navy);
            else
                row.AutoItem().Text(value).FontSize(8).FontColor(Navy);
        });
        col.Item().LineHorizontal(0.5f).LineColor(Gray200);
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
