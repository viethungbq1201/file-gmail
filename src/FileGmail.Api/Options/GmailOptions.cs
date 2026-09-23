namespace FileGmail.Api.Options;

public class GmailOptions
{
    public const string SectionName = "Gmail";

    public string CredentialsPath { get; set; } = "credentials.json";
    public string CredentialsJson { get; set; } = "";
    public string TokenPath { get; set; } = "token.json";
    public string TokenJson { get; set; } = "";
    public string RedirectUri { get; set; } = "http://localhost:5244/api/gmail/oauth/callback";
    public string FrontendBaseUrl { get; set; } = "";
    public string SenderEmail { get; set; } = "";
    public string RecipientEmail { get; set; } = "";

    public bool IsRequiredConfigured =>
        !string.IsNullOrWhiteSpace(SenderEmail) &&
        !string.IsNullOrWhiteSpace(RecipientEmail) &&
        !string.IsNullOrWhiteSpace(RedirectUri);
}