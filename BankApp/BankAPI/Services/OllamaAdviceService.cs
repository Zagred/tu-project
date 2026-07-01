using System.Net.Http.Json;
using System.Text.Json;

namespace BankAPI.Services
{
    public class OllamaAdviceService
    {
        private readonly HttpClient _httpClient;

        public OllamaAdviceService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetAdviceAsync(string prompt)
        {
            var request = new
            {
                model = "mistral",
                prompt = prompt,
                stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("api/generate", request);

            if (!response.IsSuccessStatusCode)
                return "AI assistant is currently unavailable.";

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("response")
                .GetString() ?? "No advice generated.";
        }
    }
}