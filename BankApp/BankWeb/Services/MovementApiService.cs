using BankAPP.Shared.DTOs;
using BankAPP.Shared.Constants;
using BankAPP.Shared.Models;

namespace BankWeb.Services
{
    public class MovementApiService : AuthenticatedApiClient
    {
        public MovementApiService(IHttpClientFactory factory, ApiAuthService authService)
            : base(factory, authService)
        {
        }

        public async Task<List<Movement>> GetMyMovementsAsync()
        {
            return await GetListAsync<Movement>("api/movements/me");
        }

        public async Task<List<Movement>> GetMovementsByUserAndTypeAsync(string type)
        {
            var movements = await GetMyMovementsAsync();

            if (string.IsNullOrWhiteSpace(type) || type == MovementTypes.All)
                return movements;

            return movements.Where(m => m.MovementType == type).ToList();
        }

        public async Task<decimal> GetTotalDebitAsync()
        {
            var movements = await GetMyMovementsAsync();
            return movements
                .Where(m => MovementTypes.IsExpense(m.MovementType))
                .Sum(m => m.Amount);
        }

        public async Task<decimal> GetTotalCreditAsync()
        {
            var movements = await GetMyMovementsAsync();
            return movements
                .Where(m => MovementTypes.IsIncome(m.MovementType))
                .Sum(m => m.Amount);
        }

        public async Task<bool> AddMovementAsync(CreateMovementRequest request)
        {
            return await PostJsonAsync("api/movements/me", request);
        }
    }
}
