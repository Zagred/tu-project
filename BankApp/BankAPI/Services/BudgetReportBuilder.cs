using BankShared.Constants;
using BankShared.Models;

namespace BankAPI.Services
{
    public sealed record BudgetReportSummary(
        decimal Income,
        decimal Expenses,
        decimal Remaining,
        int TransactionCount);

    public static class BudgetReportBuilder
    {
        public static BudgetReportSummary CreateSummary(IEnumerable<Movement> movements)
        {
            var movementList = movements.ToList();
            var income = movementList
                .Where(m => MovementTypes.IsIncome(m.MovementType))
                .Sum(m => m.Amount);

            var expenses = movementList
                .Where(m => MovementTypes.IsExpense(m.MovementType))
                .Sum(m => m.Amount);

            return new BudgetReportSummary(
                income,
                expenses,
                income - expenses,
                movementList.Count);
        }

        public static string BuildMonthlyEmailBody(BudgetReportSummary summary) => $"""
            Monthly Budget Report

            Income: {summary.Income:F2} BGN
            Expenses: {summary.Expenses:F2} BGN
            Remaining: {summary.Remaining:F2} BGN

            Total transactions: {summary.TransactionCount}
            """;
    }
}
