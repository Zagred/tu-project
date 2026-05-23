using System.Net.Http.Headers;
using System.Net.Http.Json;
using BankAPP.Shared.DTOs;

namespace BankAPP.Services
{
    public class AssistantApiService
    {
        private readonly HttpClient _httpClient;

        public AssistantApiService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("BankApi");
        }

        public async Task<string> GetAdviceAsync()
        {
            if (string.IsNullOrWhiteSpace(SessionManager.Token))
                return "User is not authenticated.";

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", SessionManager.Token);

            var response = await _httpClient.GetAsync("api/assistant/advice");

            if (!response.IsSuccessStatusCode)
                return "Unable to generate advice.";

            var result = await response.Content
                .ReadFromJsonAsync<FinancialAdviceResponse>();

            return result?.Advice ?? "No advice available.";
        }
    }
}