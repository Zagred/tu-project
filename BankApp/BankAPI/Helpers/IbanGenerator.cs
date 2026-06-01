using System.Security.Cryptography;

namespace BankAPI.Helpers
{
    public static class IbanGenerator
    {
        public static string Generate()
        {
            string country = "BG";
            string bankCode = "BANK"; // demo
            string accountNumber = RandomNumberGenerator.GetInt32(10_000_000, 100_000_000).ToString();

            return $"{country}{RandomNumberGenerator.GetInt32(10, 100)}{bankCode}{accountNumber}";
        }
    }
}
