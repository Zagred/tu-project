using BankAPP.Shared.DTOs;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace BankWeb.Services
{
    public class ApiAuthService
    {
        private const string StorageKey = "bankweb.login";
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private bool _initialized;

        public ApiAuthService(IHttpClientFactory factory, IJSRuntime jsRuntime)
        {
            _httpClient = factory.CreateClient("BankApiAnonymous");
            _jsRuntime = jsRuntime;
        }

        public string Token { get; private set; } = string.Empty;
        public LoginResponse? CurrentUser { get; private set; }
        public event Action? Changed;

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _initialized = true;

            var stored = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrWhiteSpace(stored))
                return;

            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(stored);
            if (loginResponse == null || string.IsNullOrWhiteSpace(loginResponse.Token))
                return;

            CurrentUser = loginResponse;
            Token = loginResponse.Token;
            Changed?.Invoke();
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/users/login", request);

            if (!response.IsSuccessStatusCode)
                return false;

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (loginResponse == null)
                return false;

            CurrentUser = loginResponse;
            Token = loginResponse.Token;
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(loginResponse));
            Changed?.Invoke();

            return true;
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/register", request);
            return response.IsSuccessStatusCode;
        }

        public bool IsAdmin =>
            string.Equals(CurrentUser?.User.Username, "admin", StringComparison.OrdinalIgnoreCase);

        public async Task LogoutAsync()
        {
            CurrentUser = null;
            Token = string.Empty;
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            Changed?.Invoke();
        }
    }
}
