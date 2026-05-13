using BankAPP.Shared.Constants;
using BankAPP.Shared.DTOs;
using BankAPP.Shared.Models;
using System.Net.Http.Json;

namespace BankAPP.Services
{
    public class MovementApiService
    {
        private readonly HttpClient _httpClient;

        public MovementApiService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("BankApi");
        }

        public async Task<List<Movement>> GetMyMovementsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<Movement>>("api/movements/me");
            return result ?? new List<Movement>();
        }

        public async Task<List<Movement>> GetMovementsByUserAndTypeAsync(string type)
        {
            var movements = await GetMyMovementsAsync();

            if (string.IsNullOrWhiteSpace(type) || type == MovementTypes.All)
                return movements;

            return movements
                .Where(m => m.MovementType == type)
                .ToList();
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
            var response = await _httpClient.PostAsJsonAsync("api/movements/me", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Movement>> GetMovementsByUserAsync()
        {
            return await GetMyMovementsAsync();
        }
    }
}
