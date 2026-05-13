using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BankWeb.Services
{
    public abstract class AuthenticatedApiClient
    {
        protected readonly HttpClient HttpClient;
        private readonly ApiAuthService _authService;

        protected AuthenticatedApiClient(IHttpClientFactory factory, ApiAuthService authService)
        {
            HttpClient = factory.CreateClient("BankApi");
            _authService = authService;
        }

        protected void ApplyToken()
        {
            HttpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(_authService.Token)
                ? null
                : new AuthenticationHeaderValue("Bearer", _authService.Token);
        }

        protected async Task<T?> GetJsonOrDefaultAsync<T>(string requestUri)
        {
            ApplyToken();
            var response = await HttpClient.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<T>();
        }

        protected async Task<List<T>> GetListAsync<T>(string requestUri)
        {
            var result = await GetJsonOrDefaultAsync<List<T>>(requestUri);
            return result ?? new List<T>();
        }

        protected async Task<bool> PostAsync(string requestUri)
        {
            ApplyToken();
            var response = await HttpClient.PostAsync(requestUri, null);
            return response.IsSuccessStatusCode;
        }

        protected async Task<bool> PostJsonAsync<TRequest>(string requestUri, TRequest request)
        {
            ApplyToken();
            var response = await HttpClient.PostAsJsonAsync(requestUri, request);
            return response.IsSuccessStatusCode;
        }

        protected async Task<TResult?> PostForResultAsync<TResult>(string requestUri)
        {
            ApplyToken();
            var response = await HttpClient.PostAsync(requestUri, null);
            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<TResult>();
        }

        protected async Task<TResult?> PostJsonForResultAsync<TRequest, TResult>(string requestUri, TRequest request)
        {
            ApplyToken();
            var response = await HttpClient.PostAsJsonAsync(requestUri, request);
            if (!response.IsSuccessStatusCode)
                return default;

            return await response.Content.ReadFromJsonAsync<TResult>();
        }

        protected async Task<(bool Success, string? ErrorMessage)> PostJsonForOutcomeAsync<TRequest>(
            string requestUri,
            TRequest request)
        {
            ApplyToken();
            var response = await HttpClient.PostAsJsonAsync(requestUri, request);
            if (response.IsSuccessStatusCode)
                return (true, null);

            return (false, await response.Content.ReadAsStringAsync());
        }
    }
}
