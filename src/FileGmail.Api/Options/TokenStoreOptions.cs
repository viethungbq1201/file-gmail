namespace FileGmail.Api.Options;

public class TokenStoreOptions
{
    public const string SectionName = "TokenStore";

    public string Store { get; set; } = "file";
    public string DatabaseUrl { get; set; } = "";

    public bool UseDatabase => Store.Equals("db", StringComparison.OrdinalIgnoreCase);
}