using MoneyTracker.Application.DTOs.Transactions;

namespace MoneyTracker.UI.Services.Print;

public interface ITransactionPrintService
{
    byte[] GenerateReport(IList<TransactionDto> transactions, string title);
}
