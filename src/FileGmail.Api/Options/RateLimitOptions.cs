namespace FileGmail.Api.Options;

public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int RequestsPerMinute { get; set; } = 10;
}