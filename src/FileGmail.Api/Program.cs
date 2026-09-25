using System.Threading.RateLimiting;
using FileGmail.Api.Middleware;
using FileGmail.Api.Models;
using FileGmail.Api.Options;
using FileGmail.Api.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var gmailOptions = BindOptions<GmailOptions>(builder, GmailOptions.SectionName);
var fileUploadOptions = BindOptions<FileUploadOptions>(builder, FileUploadOptions.SectionName);
var rateLimitOptions = BindOptions<RateLimitOptions>(builder, RateLimitOptions.SectionName);
var securityOptions = BindOptions<SecurityOptions>(builder, SecurityOptions.SectionName);
var tokenStoreOptions = BindOptions<TokenStoreOptions>(builder, TokenStoreOptions.SectionName);

if (fileUploadOptions.MaxFileSizeMb <= 0 || fileUploadOptions.MaxTotalSizeMb <= 0)
{
    throw new InvalidOperationException("MAX_FILE_SIZE_MB và MAX_TOTAL_SIZE_MB phải là số dương.");
}

if (rateLimitOptions.RequestsPerMinute <= 0)
{
    throw new InvalidOperationException("RATE_LIMIT_PER_MINUTE phải là số dương.");
}

builder.Services.AddSingleton<IOptions<GmailOptions>>(Options.Create(gmailOptions));
    builder.Services.AddSingleton<IOptions<FileUploadOptions>>(Options.Create(fileUploadOptions));
    builder.Services.AddSingleton<IOptions<RateLimitOptions>>(Options.Create(rateLimitOptions));
    builder.Services.AddSingleton<IOptions<SecurityOptions>>(Options.Create(securityOptions));
    builder.Services.AddSingleton<IOptions<TokenStoreOptions>>(Options.Create(tokenStoreOptions));

    if (tokenStoreOptions.UseDatabase)
    {
        builder.Services.AddSingleton<DbTokenDataStore>();
        builder.Services.AddSingleton<IDataStore>(sp => sp.GetRequiredService<DbTokenDataStore>());
        builder.Services.AddHostedService<TokenHealthCheckService>();
    }
    else
    {
        builder.Services.AddSingleton<IDataStore, JsonTokenDataStore>();
    }

    builder.Services.AddSingleton<GmailOAuthService>();
    builder.Services.AddSingleton<IGmailService, GmailService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = securityOptions.AllowedOrigins
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Where(static origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            Console.Error.WriteLine("CẢNH BÁO: ALLOWED_ORIGINS chưa được cấu hình. Cho phép mọi origin (chỉ nên dùng cho phát triển).");
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiResponse(false, "Bạn đang gửi quá nhiều yêu cầu. Vui lòng thử lại sau ít phút."),
            cancellationToken);
    };
    options.AddPolicy("email-send", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitOptions.RequestsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var maxRequestBodyBytes = fileUploadOptions.MaxTotalSizeBytes + 1024 * 1024;
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.MaxRequestBodySize = maxRequestBodyBytes;
});

var app = builder.Build();

if (tokenStoreOptions.UseDatabase && !string.IsNullOrWhiteSpace(gmailOptions.TokenJson))
{
    app.Services.GetRequiredService<DbTokenDataStore>().SeedFromJson(gmailOptions.TokenJson);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("frontend");
app.UseMiddleware<AccessKeyMiddleware>();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.MapGet("/", () => Results.Ok(new { name = "FileGmail Api", health = "/api/health" }));

var frontendRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (Directory.Exists(frontendRoot))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallback(async (HttpContext context) =>
    {
        if (!HttpMethods.IsGet(context.Request.Method) ||
            context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(Path.Combine(frontendRoot, "index.html"));
    });
}

app.Run();

static T BindOptions<T>(WebApplicationBuilder builder, string sectionName) where T : new()
{
    var options = builder.Configuration.GetSection(sectionName).Get<T>() ?? new T();
    ApplyEnvironmentOverrides(options);
    return options;
}

static void ApplyEnvironmentOverrides(object options)
{
    switch (options)
    {
        case GmailOptions gmail:
            SetIfPresent("GMAIL_CREDENTIALS_JSON", value => gmail.CredentialsJson = value);
            SetIfPresent("GMAIL_TOKEN_JSON", value => gmail.TokenJson = value);
            SetIfPresent("GMAIL_CREDENTIALS_PATH", value => gmail.CredentialsPath = value);
            SetIfPresent("GMAIL_TOKEN_PATH", value => gmail.TokenPath = value);
            SetIfPresent("GMAIL_REDIRECT_URI", value => gmail.RedirectUri = value);
            SetIfPresent("GMAIL_SENDER_EMAIL", value => gmail.SenderEmail = value);
            SetIfPresent("GMAIL_RECIPIENT_EMAIL", value => gmail.RecipientEmail = value);
            SetIfPresent("GMAIL_FRONTEND_URL", value => gmail.FrontendBaseUrl = value);
            break;
        case FileUploadOptions fileUpload:
            SetIfPresent("MAX_FILE_SIZE_MB", value => fileUpload.MaxFileSizeMb = ParseRequire("MAX_FILE_SIZE_MB", value));
            SetIfPresent("MAX_TOTAL_SIZE_MB", value => fileUpload.MaxTotalSizeMb = ParseRequire("MAX_TOTAL_SIZE_MB", value));
            break;
        case RateLimitOptions rateLimit:
            SetIfPresent("RATE_LIMIT_PER_MINUTE", value => rateLimit.RequestsPerMinute = ParseRequire("RATE_LIMIT_PER_MINUTE", value));
            break;
        case SecurityOptions security:
            SetIfPresent("APP_ACCESS_KEY", value => security.AccessKey = value);
            SetIfPresent("ALLOWED_ORIGINS", value => security.AllowedOrigins = value);
            break;
        case TokenStoreOptions tokenStore:
            SetIfPresent("TOKEN_STORE", value => tokenStore.Store = value);
            SetIfPresent("DATABASE_URL", value => tokenStore.DatabaseUrl = value);
            break;
    }
}

static void SetIfPresent(string name, Action<string> apply)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (!string.IsNullOrWhiteSpace(value))
    {
        apply(value);
    }
}

static int ParseRequire(string name, string value)
{
    if (!int.TryParse(value, out var parsed) || parsed <= 0)
    {
        throw new InvalidOperationException($"{name} phải là số nguyên dương, nhận được: '{value}'.");
    }

    return parsed;
}

public partial class Program { }