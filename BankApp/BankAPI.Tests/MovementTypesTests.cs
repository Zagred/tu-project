using BankShared.Constants;
using Xunit;

namespace BankAPI.Tests
{
    public class MovementTypesTests
    {
        [Theory]
        [InlineData(MovementTypes.CardPayment)]
        [InlineData(MovementTypes.CashWithdrawal)]
        [InlineData(MovementTypes.Fee)]
        public void IsExpenseReturnsTrueForExpenseTypes(string movementType)
        {
            Assert.True(MovementTypes.IsExpense(movementType));
        }

        [Theory]
        [InlineData(MovementTypes.Deposit)]
        [InlineData(MovementTypes.Transfer)]
        public void IsIncomeReturnsTrueForIncomeTypes(string movementType)
        {
            Assert.True(MovementTypes.IsIncome(movementType));
        }

        [Fact]
        public void GetDisplayNameReturnsReadableNameForKnownType()
        {
            Assert.Equal("Card Payment", MovementTypes.GetDisplayName(MovementTypes.CardPayment));
        }
    }
}
