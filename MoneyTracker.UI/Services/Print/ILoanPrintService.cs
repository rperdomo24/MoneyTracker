using MoneyTracker.Application.DTOs.Loans;

namespace MoneyTracker.UI.Services.Print;

public interface ILoanPrintService
{
    byte[] GenerateReport(IList<LoanDto> loans);
}
