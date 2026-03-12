using Microsoft.AspNetCore.Identity;

namespace MoneyTracker.Infrastructure.Persistence
{
    public class LowerInvariantLookupNormalizer : ILookupNormalizer
    {
        public string? NormalizeName(string? name)
        {
            return Normalize(name);
        }

        public string? NormalizeEmail(string? email)
        {
            return Normalize(email);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        }
    }
}
