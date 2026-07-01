using System.Net.Http.Json;
using BankShared.DTOs;

namespace BankAPP.Services
{
    public class BudgetReportApiService
    {
        private readonly HttpClient _httpClient;

        public BudgetReportApiService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("BankApi");
        }

        public async Task<string> SendMonthlyReportAsync()
        {
            if (string.IsNullOrWhiteSpace(SessionManager.Token))
                return "User is not authenticated.";

            try
            {
                var response = await _httpClient.PostAsync("api/BudgetReport/monthly-email", null);
                var result = await ReadMessageAsync(response);

                if (response.IsSuccessStatusCode)
                    return result?.Message ?? "Monthly report sent successfully.";

                return result?.Message ?? "Unable to send monthly report.";
            }
            catch (HttpRequestException)
            {
                return "Unable to send monthly report. The API is not reachable.";
            }
            catch (TaskCanceledException)
            {
                return "Unable to send monthly report. The request timed out.";
            }
        }

        private static async Task<MessageResponse?> ReadMessageAsync(HttpResponseMessage response)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch
            {
                return null;
            }
        }
    }
}
