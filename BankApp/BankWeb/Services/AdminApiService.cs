using BankAPP.Shared.DTOs;
namespace BankWeb.Services
{
    public class AdminApiService : AuthenticatedApiClient
    {
        public AdminApiService(IHttpClientFactory factory, ApiAuthService authService)
            : base(factory, authService)
        {
        }

        public async Task<List<AdminAccountDto>> GetAllAccountsAsync()
        {
            return await GetListAsync<AdminAccountDto>("api/admin/accounts");
        }

        public async Task<List<AdminCardDto>> GetUserCardsAsync()
        {
            return await GetListAsync<AdminCardDto>("api/admin/user-cards");
        }

        public async Task<List<AdminMerchantDto>> GetMerchantsAsync()
        {
            return await GetListAsync<AdminMerchantDto>("api/admin/merchants");
        }

        public async Task<List<AdminLocationDto>> GetLocationsByMerchantAsync(int merchantId)
        {
            return await GetListAsync<AdminLocationDto>($"api/admin/merchants/{merchantId}/locations");
        }

        public async Task<(bool Success, string? ErrorMessage)> CreatePosTransactionAsync(PosTransactionRequest request)
        {
            return await PostJsonForOutcomeAsync("api/admin/pos-transaction", request);
        }

        public async Task<AdminCardDto?> CreateCardAsync(CreateCardRequest request)
        {
            return await PostJsonForResultAsync<CreateCardRequest, AdminCardDto>("api/admin/cards", request);
        }

        public async Task<List<PendingTransferDto>> GetPendingTransfersAsync()
        {
            return await GetListAsync<PendingTransferDto>("api/admin/pending-transfers");
        }

        public async Task<bool> ApproveTransferAsync(int movementId)
        {
            return await PostAsync($"api/admin/transfers/{movementId}/approve");
        }

        public async Task<bool> RejectTransferAsync(int movementId)
        {
            return await PostAsync($"api/admin/transfers/{movementId}/reject");
        }
    }
}
