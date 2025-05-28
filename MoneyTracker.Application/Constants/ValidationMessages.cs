namespace MoneyTracker.Application.Constants
{
    public static class ValidationMessages
    {
        public const string Required = "This field is required.";
        public const string MaxLength = "The maximum allowed length is {0} characters.";
        public const string GreaterThanZero = "The amount must be greater than zero.";
        public const string DateNotInFuture = "The date cannot be in the future.";
    }
}
