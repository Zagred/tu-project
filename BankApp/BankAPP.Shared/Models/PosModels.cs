using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankAPP.Shared.Models
{
    [Table("cards")]
    public class Card
    {
        [Key]
        [Column("card_id")]
        public int CardId { get; set; }

        [Column("account_id")]
        public int AccountId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("masked_card_number")]
        public string MaskedCardNumber { get; set; } = string.Empty;

        [Required]
        [Column("card_type")]
        public string CardType { get; set; } = "Debit"; // Debit, Credit

        [Column("expiration_date")]
        public DateTime ExpirationDate { get; set; }

        [Required]
        [Column("status")]
        public string Status { get; set; } = "active"; // active, blocked, expired
    }

    [Table("merchants")]
    public class Merchant
    {
        [Key]
        [Column("merchant_id")]
        public int MerchantId { get; set; }

        [Required]
        [Column("merchant_name")]
        public string MerchantName { get; set; } = string.Empty;

        [Required]
        [Column("merchant_category")]
        public string MerchantCategory { get; set; } = string.Empty; // Supermarket, Gas Station, Restaurant, etc.
    }

    [Table("locations")]
    public class Location
    {
        [Key]
        [Column("location_id")]
        public int LocationId { get; set; }

        [Column("merchant_id")]
        public int MerchantId { get; set; }

        [ForeignKey("MerchantId")]
        public Merchant? Merchant { get; set; }

        [Required]
        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Column("city")]
        public string City { get; set; } = string.Empty;

        [Column("latitude")]
        public decimal? Latitude { get; set; }

        [Column("longitude")]
        public decimal? Longitude { get; set; }
    }
}
