using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankAPP.Shared.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("egn")]
        public string Egn { get; set; } = string.Empty;

        [Column("registration_date")]
        public DateTime RegistrationDate { get; set; }

        [Required]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("phone_number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;
    }
}