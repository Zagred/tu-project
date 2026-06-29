using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankAPP.Shared.Models
{
    [Table("accounts")]
    public class Account
    {
        [Key]
        [Column("account_id")]
        public int Id { get; set; }

        [Required]
        [Column("iban")]
        public string IBAN { get; set; } = string.Empty;

        [Required]
        [Column("currency")]
        public string Currency { get; set; } = "BGN";

        [Column("balance")]
        public decimal Balance { get; set; }

        [Required]
        [Column("status")]
        public string Status { get; set; } = "active";

        [Column("opened_at")]
        public DateTime OpenedAt { get; set; }
    }
}