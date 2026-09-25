using FileGmail.Api.Services;

namespace FileGmail.Api.Services;

public sealed class TokenHealthCheckService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenHealthCheckService> _logger;

    public TokenHealthCheckService(
        IServiceScopeFactory scopeFactory,
        ILogger<TokenHealthCheckService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckTokenAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kiểm tra token Gmail thất bại");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckTokenAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var oauthService = scope.ServiceProvider.GetRequiredService<GmailOAuthService>();

        try
        {
            using var client = await oauthService.GetAuthorizedGmailClientAsync(cancellationToken);
            _logger.LogInformation("Token Gmail hợp lệ (health check)");
        }
        catch (GmailNotAuthorizedException ex)
        {
            _logger.LogWarning(ex, "Token Gmail không hợp lệ hoặc thiếu. Cần kết nối lại Gmail.");
        }
    }
}