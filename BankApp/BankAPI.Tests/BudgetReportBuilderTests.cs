using BankAPI.Services;
using BankAPP.Shared.Constants;
using BankAPP.Shared.Models;
using Xunit;

namespace BankAPI.Tests
{
    public class BudgetReportBuilderTests
    {
        [Fact]
        public void CreateSummaryCalculatesIncomeExpensesAndRemaining()
        {
            var movements = new List<Movement>
            {
                CreateMovement(MovementTypes.Deposit, 500m),
                CreateMovement(MovementTypes.Transfer, 125m),
                CreateMovement(MovementTypes.CardPayment, 40m),
                CreateMovement(MovementTypes.CashWithdrawal, 30m),
                CreateMovement(MovementTypes.Fee, 5m)
            };

            var summary = BudgetReportBuilder.CreateSummary(movements);

            Assert.Equal(625m, summary.Income);
            Assert.Equal(75m, summary.Expenses);
            Assert.Equal(550m, summary.Remaining);
            Assert.Equal(5, summary.TransactionCount);
        }

        [Fact]
        public void CreateSummaryIgnoresUnknownMovementTypesForTotals()
        {
            var movements = new List<Movement>
            {
                CreateMovement("unknown", 100m)
            };

            var summary = BudgetReportBuilder.CreateSummary(movements);

            Assert.Equal(0m, summary.Income);
            Assert.Equal(0m, summary.Expenses);
            Assert.Equal(0m, summary.Remaining);
            Assert.Equal(1, summary.TransactionCount);
        }

        [Fact]
        public void BuildMonthlyEmailBodyContainsFormattedSummary()
        {
            var summary = new BudgetReportSummary(1200m, 345.5m, 854.5m, 7);

            var body = BudgetReportBuilder.BuildMonthlyEmailBody(summary);

            Assert.Contains("Monthly Budget Report", body);
            Assert.Contains("Income: 1200.00 BGN", body);
            Assert.Contains("Expenses: 345.50 BGN", body);
            Assert.Contains("Remaining: 854.50 BGN", body);
            Assert.Contains("Total transactions: 7", body);
        }

        private static Movement CreateMovement(string movementType, decimal amount) => new()
        {
            MovementType = movementType,
            Amount = amount,
            MovementDateTime = DateTime.UtcNow
        };
    }
}
