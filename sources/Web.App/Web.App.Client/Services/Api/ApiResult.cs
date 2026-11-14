using System.Net;
using System.Net.Http.Json;

namespace Web.App.Client.Services.Api
{
    /// <summary>
    /// Ergebnis eines API-Aufrufs: Wert oder verständliche Fehlermeldung.
    /// Seiten prüfen nur IsSuccess/Error und kennen weder URLs noch HTTP-Details.
    /// </summary>
    public sealed class ApiResult<T>
    {
        private ApiResult(T? value, string? error)
        {
            Value = value;
            Error = error;
        }

        public T? Value { get; }

        public string? Error { get; }

        public bool IsSuccess => Error is null;

        public static ApiResult<T> Success(T value) => new(value, null);

        public static ApiResult<T> Failure(string error) => new(default, error);
    }

    internal static class ApiResponse
    {
        private sealed class ProblemBody
        {
            public string? Detail { get; set; }
        }

        /// <summary>Wertet eine Antwort aus; Fehlertext aus ProblemDetails der API oder ein Standardtext.</summary>
        public static async Task<ApiResult<T>> ReadAsync<T>(Task<HttpResponseMessage> request, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await request;

                if (response.IsSuccessStatusCode)
                {
                    var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

                    return value is null
                        ? ApiResult<T>.Failure("Die Antwort des Servers war leer.")
                        : ApiResult<T>.Success(value);
                }

                return ApiResult<T>.Failure(await ReadErrorAsync(response, cancellationToken));
            }
            catch (HttpRequestException)
            {
                return ApiResult<T>.Failure("Der Server ist nicht erreichbar. Bitte später erneut versuchen.");
            }
        }

        private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(cancellationToken);

                if (!string.IsNullOrWhiteSpace(problem?.Detail))
                {
                    return problem.Detail;
                }
            }
            catch
            {
                // Kein ProblemDetails-Body -> Standardtext.
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Die Sitzung ist abgelaufen. Bitte erneut anmelden.",
                HttpStatusCode.Forbidden => "Keine Berechtigung.",
                HttpStatusCode.NotFound => "Nicht gefunden.",
                _ => "Die Aktion ist fehlgeschlagen."
            };
        }
    }
}
