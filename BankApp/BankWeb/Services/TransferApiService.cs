using BankAPP.Shared.DTOs;
namespace BankWeb.Services
{
    public class TransferApiService : AuthenticatedApiClient
    {
        public TransferApiService(IHttpClientFactory factory, ApiAuthService authService)
            : base(factory, authService)
        {
        }

        public async Task<bool> TransferAsync(TransferRequest request)
        {
            return await PostJsonAsync("api/transfers", request);
        }
    }
}
