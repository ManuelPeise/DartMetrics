using System.Net.Http.Json;

namespace Web.App.Components.Admin
{
    /// <summary>Liest die Fehlermeldung aus der ProblemDetails-Antwort der API (GlobalExceptionHandlerMiddleware).</summary>
    public static class ApiProblem
    {
        private sealed class ProblemBody
        {
            public string? Title { get; set; }

            public string? Detail { get; set; }
        }

        public static async Task<string> ReadMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemBody>();

                if (!string.IsNullOrWhiteSpace(problem?.Detail))
                {
                    return problem.Detail;
                }
            }
            catch
            {
                // Kein ProblemDetails-Body -> Standardtext unten.
            }

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.Forbidden => "Keine Berechtigung.",
                System.Net.HttpStatusCode.Unauthorized => "Die Sitzung ist abgelaufen. Bitte erneut anmelden.",
                System.Net.HttpStatusCode.NotFound => "Eintrag nicht gefunden.",
                _ => "Die Aktion ist fehlgeschlagen."
            };
        }
    }
}
