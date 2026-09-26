namespace MoneyTracker.Application.Interfaces
{
    /// <summary>Encrypts/decrypts secrets at rest (e.g. OAuth refresh tokens). Implemented in the UI layer via ASP.NET Core Data Protection.</summary>
    public interface ITokenProtector
    {
        string Protect(string plaintext);
        string Unprotect(string protectedText);
    }
}
