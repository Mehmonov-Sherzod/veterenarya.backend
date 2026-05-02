using System.Net;
using System.Text.Json;
using VeterinaryBackend.Domain.Exceptions;

namespace VeterinaryBackend.API.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, payload) = exception switch
        {
            ValidationAppException v => (
                (int)HttpStatusCode.BadRequest,
                (object)new ApiErrorResponse
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Validation failed.",
                    Errors = v.Errors,
                    TraceId = context.TraceIdentifier
                }),
            NotFoundException nf => (
                (int)HttpStatusCode.NotFound,
                new ApiErrorResponse
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Title = nf.Message,
                    TraceId = context.TraceIdentifier
                }),
            UnauthorizedAppException ua => (
                (int)HttpStatusCode.Unauthorized,
                new ApiErrorResponse
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = ua.Message,
                    TraceId = context.TraceIdentifier
                }),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                new ApiErrorResponse
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "An unexpected error occurred.",
                    Detail = _env.IsDevelopment() ? exception.ToString() : null,
                    TraceId = context.TraceIdentifier
                })
        };

        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
        else
            _logger.LogWarning(exception, "Handled exception. TraceId: {TraceId}", context.TraceIdentifier);

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}

public class ApiErrorResponse
{
    public int Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? TraceId { get; set; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; set; }
}
