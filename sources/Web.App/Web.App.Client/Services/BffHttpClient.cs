namespace Web.App.Client.Services
{
    public static class BffHttpClient
    {
        public const string Name = "Bff";

        /// <summary>
        /// Every /bff request must carry this header. Browsers do not allow cross-site requests with custom headers
        /// without a CORS preflight, which the server never grants -> CSRF protection for the cookie.
        /// </summary>
        public const string CsrfHeaderName = "X-CSRF";
    }

    public sealed class CsrfHeaderHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.TryAddWithoutValidation(BffHttpClient.CsrfHeaderName, "1");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
