using System.Security.Cryptography;
using System.Text;

namespace BankAPP.Services
{
    public static class SecurityService
    {
        public static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();

            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(bytes);

            var builder = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}