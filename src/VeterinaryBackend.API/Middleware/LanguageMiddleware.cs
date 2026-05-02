using Microsoft.Extensions.Primitives;
using VeterinaryBackend.Business.Helpers;
using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.API.Middleware;

public class LanguageMiddleware
{
    private const string LanguageItemKey = "RequestLanguage";
    private const string QueryKey = "lang";
    private const string HeaderKey = "Accept-Language";

    private readonly RequestDelegate _next;

    public LanguageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var code = ResolveLanguageCode(context);
        var language = LanguageHelper.Parse(code);

        context.Items[LanguageItemKey] = language;
        context.Response.Headers["Content-Language"] = LanguageHelper.ToCode(language);

        await _next(context);
    }

    public static Language GetLanguage(HttpContext context)
    {
        if (context.Items.TryGetValue(LanguageItemKey, out var value) && value is Language lang)
            return lang;

        return Language.Uz;
    }

    private static string? ResolveLanguageCode(HttpContext context)
    {
        if (context.Request.Query.TryGetValue(QueryKey, out var queryValue) && !StringValues.IsNullOrEmpty(queryValue))
            return queryValue.ToString();

        if (context.Request.Headers.TryGetValue(HeaderKey, out var headerValue) && !StringValues.IsNullOrEmpty(headerValue))
        {
            var raw = headerValue.ToString();
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Split(';')[0].Trim())
                .FirstOrDefault();
        }

        return null;
    }
}
