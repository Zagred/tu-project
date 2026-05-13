using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BankAPP.Shared.Constants;

namespace BankAPP.Shared.Models
{
    [Table("movements")]
    public class Movement
    {
        [Key]
        [Column("movement_id")]
        public int MovementId { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [ForeignKey("AccountId")]
        public Account? Account { get; set; }

        [Column("card_id")]
        public int? CardId { get; set; }

        [Column("merchant_id")]
        public int? MerchantId { get; set; }

        [Column("location_id")]
        public int? LocationId { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Required]
        [Column("currency")]
        public string Currency { get; set; } = "BGN";

        [Required]
        [Column("movement_type")]
        public string MovementType { get; set; } = string.Empty;

        [NotMapped]
        public string MovementTypeDisplay => MovementTypes.GetDisplayName(MovementType);

        [NotMapped]
        public bool IsExpense => MovementTypes.IsExpense(MovementType);

        [NotMapped]
        public string AmountText => (IsExpense ? "-" : "+") + Amount.ToString("F2");

        [NotMapped]
        public string Category => IsExpense ? "Expense" : "Income";

        [Required]
        [Column("status")]
        public string Status { get; set; } = "completed";

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("reference_number")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Column("movement_datetime")]
        public DateTime MovementDateTime { get; set; }
    }
}
