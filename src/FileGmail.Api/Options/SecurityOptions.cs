namespace FileGmail.Api.Options;

public class SecurityOptions
{
    public const string SectionName = "Security";

    public string AccessKey { get; set; } = "";
    public string AllowedOrigins { get; set; } = "";
}