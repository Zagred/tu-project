using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankAPP.Shared.DTOs
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Egn { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}