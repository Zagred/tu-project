using BankAPP.Shared.DTOs;
using System.Net.Http.Json;

namespace BankAPP.Services
{
    public class AccountApiService
    {
        private readonly HttpClient _httpClient;

        public AccountApiService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("BankApi");
        }

        public async Task<List<AccountDto>> GetMyAccountsAsync()
        {
            var accounts = await _httpClient.GetFromJsonAsync<List<AccountDto>>("api/accounts/me");
            return accounts ?? new List<AccountDto>();
        }

        public async Task<AccountDto?> GetAccountByIbanAsync(string iban)
        {
            var response = await _httpClient.GetAsync($"api/accounts/by-iban/{iban}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<AccountDto>();
        }

        public async Task<AccountDto?> CreateAccountAsync()
        {
            var response = await _httpClient.PostAsync("api/accounts/me", null);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<AccountDto>();
        }
    }
}

