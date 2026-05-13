using System.Net.Http.Headers;

namespace BankAPP.Services
{
    public class AuthMessageHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(SessionManager.Token))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", SessionManager.Token);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}