using BankShared.DTOs;
namespace BankWeb.Services
{
    public class TransferApiService : AuthenticatedApiClient
    {
        public TransferApiService(IHttpClientFactory factory, ApiAuthService authService)
            : base(factory, authService)
        {
        }

        public async Task<(bool Success, string? ErrorMessage)> TransferAsync(TransferRequest request)
        {
            return await PostJsonForOutcomeAsync("api/transfers", request);
        }
    }
}
