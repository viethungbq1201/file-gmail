using FileGmail.Api.Middleware;
using FileGmail.Api.Models;
using FileGmail.Api.Options;
using FileGmail.Api.Services;
using FileGmail.Api.Validators;
using Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FileGmail.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GmailController : ControllerBase
{
    private readonly GmailOAuthService _oauthService;
    private readonly IGmailService _gmailService;
    private readonly FileUploadOptions _fileUploadOptions;
    private readonly GmailOptions _gmailOptions;
    private readonly ILogger<GmailController> _logger;

    public GmailController(
        GmailOAuthService oauthService,
        IGmailService gmailService,
        IOptions<FileUploadOptions> fileUploadOptions,
        IOptions<GmailOptions> gmailOptions,
        ILogger<GmailController> logger)
    {
        _oauthService = oauthService;
        _gmailService = gmailService;
        _fileUploadOptions = fileUploadOptions.Value;
        _gmailOptions = gmailOptions.Value;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var authorized = await _oauthService.IsAuthorizedAsync(cancellationToken);
        return Ok(new ApiResponse(
            true,
            Data: new GmailStatusDto(
                authorized,
                _gmailOptions.SenderEmail,
                _gmailOptions.RecipientEmail)));
    }

    [HttpGet("access-key/verify")]
    public IActionResult VerifyAccessKey() => Ok(new ApiResponse(true));

    [HttpGet("auth")]
    public async Task<IActionResult> StartAuth(CancellationToken cancellationToken)
    {
        if (!_gmailOptions.IsRequiredConfigured)
        {
            return Redirect(FrontendUrl(_gmailOptions, "?gmailAuthorized=false&error=not_configured"));
        }

        var url = await _oauthService.GetAuthorizationUrlAsync(cancellationToken);
        return Redirect(url);
    }

    [HttpGet("oauth/callback")]
    public async Task<IActionResult> OAuthCallback(
        [FromQuery] string? code,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        var redirectUrl = FrontendUrl(_gmailOptions, "?gmailAuthorized=false");

        var errorDescription = Request.Query["error_description"].FirstOrDefault();
        if (error is not null || code is null)
        {
            _logger.LogWarning("OAuth callback failed: error='{Error}' description='{Description}'",
                error, errorDescription);
            return Redirect(redirectUrl);
        }

        try
        {
            await _oauthService.CompleteAuthorizationAsync(code, cancellationToken);
            redirectUrl = FrontendUrl(_gmailOptions, "?gmailAuthorized=true");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed khi trao đổi authorization code");
        }

        return Redirect(redirectUrl);
    }

    [HttpPost("send")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(int.MaxValue)]
    public async Task<ActionResult<ApiResponse>> Send(
        [FromForm] IFormFileCollection files,
        [FromForm] string? subject,
        [FromForm] string? body,
        CancellationToken cancellationToken)
    {
        var validationResult = FileValidator.Validate(files, _fileUploadOptions);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("File validation failed: {Error}", validationResult.ErrorMessage);
            return BadRequest(new ApiResponse(false, validationResult.ErrorMessage));
        }

        try
        {
            await _gmailService.SendEmailAsync(
                files,
                string.IsNullOrWhiteSpace(subject) ? EmailDefaults.Subject : subject,
                string.IsNullOrWhiteSpace(body) ? EmailDefaults.Body : body,
                cancellationToken);
            return Ok(new ApiResponse(true, "Đã gửi email thành công."));
        }
        catch (GmailNotAuthorizedException)
        {
            return Unauthorized(new ApiResponse(false, "Gmail chưa được kết nối. Vui lòng kết nối Gmail trước."));
        }
        catch (GoogleApiException ex)
        {
            _logger.LogError(ex, "Gmail API error khi gửi email. HTTP status {StatusCode}", ex.HttpStatusCode);
            return StatusCode(502, new ApiResponse(false, "Không thể gửi email lúc này. Vui lòng thử lại sau."));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, new ApiResponse(false, "Yêu cầu bị huỷ."));
        }
    }

    private static string FrontendUrl(GmailOptions options, string query)
    {
        var baseUrl = options.FrontendBaseUrl.TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? "/" + query : baseUrl + "/" + query;
    }
}