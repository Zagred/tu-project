using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankAPP.Shared.Models
{
    [Table("user_accounts")]
    public class UserAccount
    {
        [Key]
        [Column("user_account_id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("role")]
        public string Role { get; set; } = string.Empty;
    }
}