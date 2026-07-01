using BankShared.DTOs;
using System.Net.Http.Json;

namespace BankAPP.Services
{
    public class TransferApiService
    {
        private readonly HttpClient _httpClient;

        public TransferApiService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("BankApi");
        }

        public async Task<(bool Success, string? ErrorMessage)> TransferAsync(TransferRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/transfers", request);
                if (response.IsSuccessStatusCode)
                    return (true, null);

                return (false, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
