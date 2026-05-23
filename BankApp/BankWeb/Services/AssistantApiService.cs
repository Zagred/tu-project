using System.Net.Http.Headers;
using System.Net.Http.Json;
using BankAPP.Shared.DTOs;

namespace BankWeb.Services
{
    public class AssistantApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiAuthService _authService;

        public AssistantApiService(
            IHttpClientFactory factory,
            ApiAuthService authService)
        {
            _httpClient = factory.CreateClient("BankApi");
            _authService = authService;
        }

        public async Task<string> GetAdviceAsync()
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authService.Token);

            var response = await _httpClient.GetAsync("api/assistant/advice");

            if (!response.IsSuccessStatusCode)
                return "Unable to generate advice.";

            var result = await response.Content
                .ReadFromJsonAsync<FinancialAdviceResponse>();

            return result?.Advice ?? "No advice available.";
        }
    }
}
