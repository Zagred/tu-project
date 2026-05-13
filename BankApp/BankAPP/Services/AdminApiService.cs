using BankAPP.Shared.DTOs;
using System.Net.Http.Json;

namespace BankAPP.Services
{
    public class AdminApiService
    {
        private readonly HttpClient _httpClient;

        public AdminApiService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("BankApi");
        }

        public async Task<List<AdminAccountDto>> GetAllAccountsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<AdminAccountDto>>("api/admin/accounts");
            return result ?? new List<AdminAccountDto>();
        }

        public async Task<List<AdminCardDto>> GetUserCardsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<AdminCardDto>>("api/admin/user-cards");
            return result ?? new List<AdminCardDto>();
        }

        public async Task<List<AdminMerchantDto>> GetMerchantsAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<AdminMerchantDto>>("api/admin/merchants");
            return result ?? new List<AdminMerchantDto>();
        }

        public async Task<List<AdminLocationDto>> GetLocationsByMerchantAsync(int merchantId)
        {
            var result = await _httpClient.GetFromJsonAsync<List<AdminLocationDto>>($"api/admin/merchants/{merchantId}/locations");
            return result ?? new List<AdminLocationDto>();
        }

        public async Task<(bool Success, string? ErrorMessage)> CreatePosTransactionAsync(PosTransactionRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/admin/pos-transaction", request);
                if (response.IsSuccessStatusCode)
                    return (true, null);

                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, errorContent);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<AdminCardDto?> CreateCardAsync(CreateCardRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/admin/cards", request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AdminCardDto>();
        }

        public async Task<List<PendingTransferDto>> GetPendingTransfersAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<PendingTransferDto>>("api/admin/pending-transfers");
            return result ?? new List<PendingTransferDto>();
        }

        public async Task<bool> ApproveTransferAsync(int movementId)
        {
            var response = await _httpClient.PostAsync($"api/admin/transfers/{movementId}/approve", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RejectTransferAsync(int movementId)
        {
            var response = await _httpClient.PostAsync($"api/admin/transfers/{movementId}/reject", null);
            return response.IsSuccessStatusCode;
        }
    }
}
