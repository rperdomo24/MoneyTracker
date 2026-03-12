namespace MoneyTracker.Application.Common
{
    public static class OperationMessages
    {
        public const string Created = "The record was created successfully.";
        public const string Updated = "The record was updated successfully.";
        public const string Deleted = "The record was deleted successfully.";
        public const string NotFound = "The requested record was not found.";
        public const string DuplicateName = "A record with the same name already exists.";
        public const string DataRetrieved = "Data retrieved successfully.";
        public const string UnexpectedError = "An unexpected error occurred. Please try again later.";
        public const string AlreadyExists = "A record with the same name already exists.";
        public const string ValidationError = "Please fill out the form correctly.";
        public const string UpdateError = "An error occurred while updating the record.";


        public const string TransferCreated = "Transfer created successfully";
        public const string TransferSameAccount = "Cannot transfer to the same account";
        public const string TransferInvalidAmount = "Amount must be greater than zero";
        public const string TransferSourceNotFound = "Source account not found";
        public const string TransferDestinationNotFound = "Destination account not found";

        public const string TransferNotValid = "This is not a valid transfer transaction";
        public const string TransferPairNotFound = "Paired transaction not found";

        public const string TransactionDuplicated = "Transaction duplicated successfully";
        public const string TransferDuplicated = "Transfer duplicated successfully";
        public const string CreditPaymentCannotDuplicate = "Credit payments cannot be duplicated";
    }
}
