using System.Net.Http.Json;
using BankAPP.Shared.DTOs;

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
            ApplyToken();
            var response = await HttpClient.PostAsync("api/BudgetReport/monthly-email", null);

            var result = await ReadMessageAsync(response);
            if (response.IsSuccessStatusCode)
                return result?.Message ?? "Monthly report sent successfully.";

            return result?.Message ?? "Unable to send monthly report.";
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
