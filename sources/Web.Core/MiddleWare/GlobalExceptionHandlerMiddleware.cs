using Microsoft.AspNetCore.Mvc;

namespace Web.Core.MiddleWare
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unhandled exception occurred. Path: {Path}",
                    context.Request.Path);

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();

                var statusCode = exception switch
                {
                    ArgumentException => StatusCodes.Status400BadRequest,
                    KeyNotFoundException => StatusCodes.Status404NotFound,
                    UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status500InternalServerError
                };

                var problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = statusCode switch
                    {
                        400 => "Bad Request",
                        403 => "Forbidden",
                        404 => "Not Found",
                        _ => "Internal Server Error"
                    },
                    Detail = statusCode == 500
                        ? "An unexpected error occurred."
                        : exception.Message,
                    Instance = context.Request.Path
                };

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(problem);
            }
        }
    }
}
