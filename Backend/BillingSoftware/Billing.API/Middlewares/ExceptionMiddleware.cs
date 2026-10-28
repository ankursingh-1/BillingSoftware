using System.Text.Json;
using Billing.Shared.Exceptions;
using Billing.Shared.Responses;

namespace Billing.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BusinessException ex)
            {
                // Expected, user-correctable failure: safe to return the message.
                _logger.LogWarning("Business rule failed: {Message}", ex.Message);

                await WriteAsync(context, ex.StatusCode, new ApiErrorResponse
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                var response = new ApiErrorResponse
                {
                    Message = "An unexpected error occurred."
                };

                // Internal details (SQL errors, stack information, etc.)
                // are only returned while developing.
                if (_environment.IsDevelopment())
                {
                    response.Errors.Add(ex.Message);
                }

                await WriteAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    response);
            }
        }

        private static async Task WriteAsync(
            HttpContext context,
            int statusCode,
            ApiErrorResponse response)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(
                    response,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
        }
    }
}
