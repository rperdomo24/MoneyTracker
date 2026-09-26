using Microsoft.AspNetCore.DataProtection;
using MoneyTracker.Application.Interfaces;

namespace MoneyTracker.Infrastructure.Services
{
    public class DataProtectionTokenProtector : ITokenProtector
    {
        private const string Purpose = "MoneyTracker.GmailConnection.RefreshToken.v1";

        private readonly IDataProtector _protector;

        public DataProtectionTokenProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector(Purpose);
        }

        public string Protect(string plaintext) => _protector.Protect(plaintext);

        public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
    }
}
