using BankAPP.Shared.Models;

namespace BankAPP.Shared.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public User User { get; set; } = new();
    }
}