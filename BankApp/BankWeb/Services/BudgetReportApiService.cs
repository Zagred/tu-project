using System.Net.Http.Json;
using BankShared.DTOs;

namespace BankWeb.Services
{
    public class BudgetReportApiService : AuthenticatedApiClient
    {
        public BudgetReportApiService(IHttpClientFactory factory, ApiAuthService authService)
            : base(factory, authService)
        {
        }

        public async Task<string> SendMonthlyReportAsync()
        {
            try
            {
                ApplyToken();
                var response = await HttpClient.PostAsync("api/BudgetReport/monthly-email", null);
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
