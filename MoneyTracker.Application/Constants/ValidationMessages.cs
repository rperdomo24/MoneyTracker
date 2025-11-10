
namespace MoneyTracker.Application.Constants
{
    public static class ValidationMessages
    {
        public const string Required = "This field is required.";
        public const string MaxLength = "The maximum allowed length is {0} characters.";
        public const string GreaterThanZero = "The amount must be greater than zero.";
        public const string DateNotInFuture = "The date cannot be in the future.";
        public const string DateRequired = "The date is required.";
        public const string AmountNotNegative = "Cannot be negative.";

        public const string NameRequired = "The name is required.";
        public const string NameMaxLength = "The name must not exceed 100 characters.";
        public const string IconMaxLength = "The icon must not exceed 50 characters.";
        public const string ColorMaxLength = "The color must not exceed 10 characters.";
        public const string NotesMaxLength = "The notes must not exceed 255 characters.";
        public const string TypeRequired = "A valid type must be selected.";


        public const string AccountRequired = "Account is required.";
        public const string SourceAccountRequired = "Source account is required.";
        public const string DestinationAccountRequired = "Destination account is required.";
        public const string SameAccountTransfer = "Cannot transfer to the same account.";
        public const string DescriptionMaxLength = "The description must not exceed 500 characters.";
    }
}
