using Google;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using FileGmail.Api.Options;
using Microsoft.Extensions.Options;

namespace FileGmail.Api.Services;

public class GmailOAuthService
{
    private const string AppName = "FileGmail";
    private const string UserId = "me";

    private readonly GmailOptions _options;
    private readonly JsonTokenDataStore _tokenStore;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GmailOAuthService> _logger;
    private readonly Lazy<Task<GoogleAuthorizationCodeFlow>> _flowLoader;

    public GmailOAuthService(
        IOptions<GmailOptions> options,
        JsonTokenDataStore tokenStore,
        IHostEnvironment environment,
        ILogger<GmailOAuthService> logger)
    {
        _options = options.Value;
        _tokenStore = tokenStore;
        _environment = environment;
        _logger = logger;
        _flowLoader = new Lazy<Task<GoogleAuthorizationCodeFlow>>(CreateFlowAsync);
    }

    public async Task<string> GetAuthorizationUrlAsync(CancellationToken cancellationToken)
    {
        var flow = await _flowLoader.Value;
        _logger.LogInformation("OAuth started");
        return flow.CreateAuthorizationCodeRequest(_options.RedirectUri).Build().ToString();
    }

    public async Task CompleteAuthorizationAsync(string code, CancellationToken cancellationToken)
    {
        var flow = await _flowLoader.Value;
        var token = await flow.ExchangeCodeForTokenAsync(UserId, code, _options.RedirectUri, cancellationToken);
        await _tokenStore.StoreAsync(UserId, token);
        _logger.LogInformation("OAuth completed và token đã được lưu");
    }

    public async Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        _logger.LogInformation("Gmail authorization status: {Authorized}", token is not null);
        return token is not null;
    }

    public async Task<Google.Apis.Gmail.v1.GmailService> GetAuthorizedGmailClientAsync(CancellationToken cancellationToken)
    {
        var flow = await _flowLoader.Value;
        var token = await GetTokenAsync()
            ?? throw new GmailNotAuthorizedException("Gmail chưa được kết nối.");

        var credential = new UserCredential(flow, UserId, token);
        try
        {
            await credential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is TokenResponseException or GoogleApiException)
        {
            await _tokenStore.DeleteAsync<TokenResponse>(UserId);
            _logger.LogWarning(ex, "Refresh token không hợp lệ, đã xoá token đã lưu");
            throw new GmailNotAuthorizedException("Vui lòng kết nối lại Gmail.", ex);
        }

        if (string.IsNullOrEmpty(credential.Token.AccessToken))
        {
            await _tokenStore.DeleteAsync<TokenResponse>(UserId);
            throw new GmailNotAuthorizedException("Vui lòng kết nối lại Gmail.");
        }

        await _tokenStore.StoreAsync(UserId, credential.Token);

        return new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = AppName
        });
    }

    private Task<TokenResponse?> GetTokenAsync() =>
        _tokenStore.GetAsync<TokenResponse>(UserId);

    private async Task<GoogleAuthorizationCodeFlow> CreateFlowAsync()
    {
        var secrets = await LoadClientSecretsAsync();
        return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = secrets,
            Scopes = [Google.Apis.Gmail.v1.GmailService.Scope.GmailSend],
            DataStore = _tokenStore
        });
    }

    private Task<ClientSecrets> LoadClientSecretsAsync()
    {
        if (!string.IsNullOrWhiteSpace(_options.CredentialsJson))
        {
            _logger.LogInformation("Sử dụng credentials từ GMAIL_CREDENTIALS_JSON");
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_options.CredentialsJson));
            return Task.FromResult(GoogleClientSecrets.FromStream(stream).Secrets);
        }

        var path = ResolveCredentialsPath();
        _logger.LogInformation("Sử dụng credentials từ {CredentialsPath}", path);
        return Task.FromResult(GoogleClientSecrets.FromFile(path).Secrets);
    }

    private string ResolveCredentialsPath()
    {
        var contentRoot = Directory.GetParent(_environment.ContentRootPath);
        var repoRoot = contentRoot?.Parent;
        var candidates = new[]
        {
            Path.GetFullPath(_options.CredentialsPath),
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.CredentialsPath)),
            contentRoot is null ? null : Path.GetFullPath(Path.Combine(contentRoot.FullName, _options.CredentialsPath)),
            repoRoot is null ? null : Path.GetFullPath(Path.Combine(repoRoot.FullName, _options.CredentialsPath))
        };

        foreach (var candidate in candidates)
        {
            if (candidate is not null && File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"Không tìm thấy file credentials. Hãy đặt file tại '{Path.GetFileName(_options.CredentialsPath)}' trong thư mục project hoặc cấu hình GMAIL_CREDENTIALS_JSON.",
            candidates[0]);
    }
}