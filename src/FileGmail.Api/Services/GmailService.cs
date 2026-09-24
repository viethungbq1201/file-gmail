using System.Text;
using FileGmail.Api.Options;
using FileGmail.Api.Validators;
using Microsoft.Extensions.Options;

namespace FileGmail.Api.Services;

public interface IGmailService
{
    Task SendEmailAsync(IEnumerable<IFormFile> files, string subject, string body, CancellationToken cancellationToken);
}

public class GmailService : IGmailService
{
    private readonly GmailOptions _options;
    private readonly GmailOAuthService _oauthService;
    private readonly ILogger<GmailService> _logger;

    public GmailService(
        IOptions<GmailOptions> options,
        GmailOAuthService oauthService,
        ILogger<GmailService> logger)
    {
        _options = options.Value;
        _oauthService = oauthService;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        IEnumerable<IFormFile> files,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var attachments = await ReadAttachmentsAsync(files, cancellationToken);
        var raw = EmailMessageBuilder.Build(
            _options.SenderEmail,
            _options.RecipientEmail,
            subject,
            body,
            attachments,
            cancellationToken);
        var rawBase64Url = EmailMessageBuilder.ToBase64Url(raw);

        var gmail = await _oauthService.GetAuthorizedGmailClientAsync(cancellationToken);
        var message = new Google.Apis.Gmail.v1.Data.Message { Raw = rawBase64Url };

        _logger.LogInformation("Email send started: {AttachmentCount} file(s), subject '{Subject}'",
            attachments.Count, subject);

        try
        {
            await gmail.Users.Messages.Send(message, "me").ExecuteAsync(cancellationToken);
            _logger.LogInformation("Email send completed: {AttachmentCount} file(s)", attachments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send failed: {AttachmentCount} file(s)", attachments.Count);
            throw;
        }
    }

    private static async Task<IReadOnlyList<AttachmentData>> ReadAttachmentsAsync(
        IEnumerable<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var result = new List<AttachmentData>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);

            result.Add(new AttachmentData(
                file.FileName,
                FileValidator.GetAllowedContentTypes(file.FileName)?.FirstOrDefault() ?? "application/octet-stream",
                memoryStream.ToArray()));
        }

        return result;
    }
}