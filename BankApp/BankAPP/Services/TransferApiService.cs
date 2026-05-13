using BankAPP.Shared.DTOs;
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

        public async Task<bool> TransferAsync(TransferRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/transfers", request);
            return response.IsSuccessStatusCode;
        }
    }
}