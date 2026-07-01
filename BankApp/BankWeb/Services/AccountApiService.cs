using BankShared.DTOs;
using BankShared.Utilities;
namespace BankWeb.Services
{
    public class AccountApiService : AuthenticatedApiClient
    {
        public AccountApiService(IHttpClientFactory factory, ApiAuthService authService)
            : base(factory, authService)
        {
        }

        public async Task<List<AccountDto>> GetMyAccountsAsync()
        {
            return await GetListAsync<AccountDto>("api/accounts/me");
        }

        public async Task<AccountDto?> GetAccountByIbanAsync(string iban)
        {
            var normalizedIban = BankInputNormalizer.NormalizeIban(iban);
            if (string.IsNullOrWhiteSpace(normalizedIban))
                return null;

            return await GetJsonOrDefaultAsync<AccountDto>($"api/accounts/by-iban/{Uri.EscapeDataString(normalizedIban)}");
        }

        public async Task<AccountDto?> CreateAccountAsync()
        {
            return await PostForResultAsync<AccountDto>("api/accounts/me");
        }
    }
}
