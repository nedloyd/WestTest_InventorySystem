using InventorySystem.API.Middleware;
using System.Net;
using System.Text.Json;

namespace InventorySystem.API.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context); 
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                await WriteErrorResponseAsync(context, ex);
            }
        }

        private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            // Map known exception types to appropriate HTTP status codes
            context.Response.StatusCode = ex switch
            {
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var errorResponse = new
            {
                status = context.Response.StatusCode,
                message = context.Response.StatusCode == 500
                    ? "An unexpected error occurred. Please try again later."
                    : ex.Message,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

    }
}
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}