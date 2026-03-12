using System.Text.RegularExpressions;
using MoneyTracker.Application.DTOs;

namespace MoneyTracker.UI.Helpers.TextImport;

public static class AccountHintResolver
{
    private static readonly Regex Last4Regex = new(@"\b(\d{4})\b", RegexOptions.Compiled);

    // Prefer "known contexts" first (card/account patterns),
    // then fallback to any 4 digits excluding years (19xx / 20xx).
    private static readonly Regex ContextLast4Regex = new(
        @"(AMEX\s+(?<last4>\d{4})|(Titular|Adicional)\s+(?<last4>\d{4})|Cuenta.*?(?<last4>\d{4}))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? ExtractHintFromText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return null;

        // 1) Context-based match (best)
        var ctx = ContextLast4Regex.Match(rawText);
        if (ctx.Success && ctx.Groups["last4"].Success)
            return ctx.Groups["last4"].Value;

        // 2) Fallback: any 4 digits BUT ignore years like 19xx/20xx
        var matches = Last4Regex.Matches(rawText);
        foreach (Match m in matches)
        {
            var v = m.Groups[1].Value;

            // ignore years
            if (v.StartsWith("19") || v.StartsWith("20"))
                continue;

            return v;
        }

        return null;
    }

    public static string? ExtractHintFromAccountName(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName)) return null;

        var matches = Last4Regex.Matches(accountName);
        if (matches.Count == 0) return null;

        // Your standard: use the last 4-digit group in the account name
        return matches[^1].Groups[1].Value;
    }

    public static int? ResolveAccountId(string? hint, IEnumerable<AccountDto> accounts)
    {
        if (string.IsNullOrWhiteSpace(hint)) return null;

        // Ignore years if they slip in
        if (hint.StartsWith("19") || hint.StartsWith("20"))
            return null;

        foreach (var account in accounts)
        {
            var last4 = ExtractHintFromAccountName(account.Name);
            if (!string.IsNullOrWhiteSpace(last4) && last4 == hint)
                return account.Id;
        }

        return null;
    }
}
