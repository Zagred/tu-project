using System.Globalization;
using System.Text.Json;

namespace BankShared.Utilities
{
    public static class BankInputNormalizer
    {
        public static string NormalizeIban(string? iban)
        {
            if (string.IsNullOrWhiteSpace(iban))
                return string.Empty;

            var chars = iban
                .Where(c => !char.IsWhiteSpace(c) && c != '-')
                .Select(char.ToUpperInvariant)
                .ToArray();

            return new string(chars);
        }

        public static bool TryParseAmount(string? value, out decimal amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized = value.Trim()
                .Replace(" ", string.Empty)
                .Replace(',', '.');

            return decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out amount);
        }

        public static string ToDisplayError(string? error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return "Request failed.";

            var trimmed = error.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal))
                return trimmed.Trim('"');

            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (document.RootElement.TryGetProperty("message", out var message))
                    return message.GetString() ?? "Request failed.";
            }
            catch
            {
                return trimmed;
            }

            return trimmed;
        }
    }
}
