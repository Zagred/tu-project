using BankAPP.Shared.DTOs;
using System.Net.Http.Json;

namespace BankAPP.Services
{
    public class UserApiService
    {
        private readonly HttpClient _httpClient;

        public UserApiService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("BankApi");
        }

        public async Task<LoginResponse?> LoginAsync(string username, string password)
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/users/login", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/register", request);
            return response.IsSuccessStatusCode;
        }
    }
}