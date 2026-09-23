using System.Security.Cryptography;
using System.Text;
using FileGmail.Api.Models;
using FileGmail.Api.Options;
using Microsoft.Extensions.Options;

namespace FileGmail.Api.Middleware;

public class AccessKeyMiddleware
{
    private const string HeaderName = "X-App-Key";

    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;
    private readonly ILogger<AccessKeyMiddleware> _logger;

    public AccessKeyMiddleware(
        RequestDelegate next,
        IOptions<SecurityOptions> options,
        ILogger<AccessKeyMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isPreflight = HttpMethods.IsOptions(context.Request.Method);
        if (!isPreflight && RequiresAccessKey(context.Request.Path))
        {
            if (!IsAuthorized(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new ApiResponse(false, "Mã truy cập không hợp lệ."));
                return;
            }
        }

        await _next(context);
    }

    private static bool RequiresAccessKey(PathString path) =>
        path.StartsWithSegments("/api/gmail/send") ||
        path.StartsWithSegments("/api/gmail/access-key/verify");

    private bool IsAuthorized(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessKey))
        {
            _logger.LogWarning("APP_ACCESS_KEY chưa được cấu hình. Endpoint gửi email đang không có mã truy cập.");
            return true;
        }

        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(supplied)) return false;

        return FixedTimeEquals(_options.AccessKey, supplied);
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var actualBytes = SHA256.HashData(Encoding.UTF8.GetBytes(actual));
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}