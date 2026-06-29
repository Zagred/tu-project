namespace BankAPP.Shared.Constants
{
    public static class MovementTypes
    {
        public const string All = "all";
        public const string Deposit = "deposit";
        public const string Transfer = "transfer";
        public const string CardPayment = "card_payment";
        public const string CashWithdrawal = "cash_withdrawal";
        public const string Fee = "fee";

        public static readonly string[] ExpenseTypes =
        {
            CardPayment,
            CashWithdrawal,
            Fee
        };

        public static readonly string[] IncomeTypes =
        {
            Deposit,
            Transfer
        };

        public static bool IsExpense(string movementType) =>
            ExpenseTypes.Contains(movementType);

        public static bool IsIncome(string movementType) =>
            IncomeTypes.Contains(movementType);

        public static string GetDisplayName(string movementType) => movementType switch
        {
            Deposit => "Deposit",
            Transfer => "Transfer",
            CardPayment => "Card Payment",
            CashWithdrawal => "Cash Withdrawal",
            Fee => "Fee",
            _ => movementType
        };
    }
}
