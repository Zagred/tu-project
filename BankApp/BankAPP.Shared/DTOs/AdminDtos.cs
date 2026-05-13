namespace BankAPP.Shared.DTOs
{
    public class PosTransactionRequest
    {
        public int CardId { get; set; }
        public int LocationId { get; set; }
        public decimal Amount { get; set; }
    }

    public class CreateCardRequest
    {
        public int AccountId { get; set; }
        public string CardType { get; set; } = "Debit";
    }

    public class TransferApprovalRequest
    {
        public int MovementId { get; set; }
        public bool Approve { get; set; }
    }

    public class AdminAccountDto
    {
        public int AccountId { get; set; }
        public string Iban { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public string DisplayText => $"{Username} - {Iban}";
    }

    public class AdminCardDto
    {
        public int CardId { get; set; }
        public int AccountId { get; set; }
        public string MaskedCardNumber { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string AccountIban { get; set; } = string.Empty;

        public string DisplayText => $"{MaskedCardNumber} ({AccountIban})";
    }

    public class AdminMerchantDto
    {
        public int MerchantId { get; set; }
        public string MerchantName { get; set; } = string.Empty;
        public string MerchantCategory { get; set; } = string.Empty;
    }

    public class AdminLocationDto
    {
        public int LocationId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        public string DisplayText => string.IsNullOrWhiteSpace(City)
            ? Address
            : $"{Address}, {City}";
    }

    public class PendingTransferDto
    {
        public int MovementId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime MovementDateTime { get; set; }
        public string FromAccount { get; set; } = string.Empty;
    }
}
