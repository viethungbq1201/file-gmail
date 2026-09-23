namespace FileGmail.Api.Options;

public class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    private const int BytesPerMb = 1024 * 1024;

    public int MaxFileSizeMb { get; set; } = 20;
    public int MaxTotalSizeMb { get; set; } = 25;

    public long MaxFileSizeBytes => (long)MaxFileSizeMb * BytesPerMb;
    public long MaxTotalSizeBytes => (long)MaxTotalSizeMb * BytesPerMb;
}