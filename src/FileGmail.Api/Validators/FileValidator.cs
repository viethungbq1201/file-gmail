using System.Text;
using FileGmail.Api.Options;
using Microsoft.AspNetCore.Http;

namespace FileGmail.Api.Validators;

public sealed record FileValidationResult(bool IsValid, string? ErrorMessage = null);

public static class FileValidator
{
    private static readonly Dictionary<string, string[]> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
        [".webp"] = ["image/webp"],
        [".pdf"] = ["application/pdf"],
        [".docx"] =
        [
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/octet-stream",
            "application/x-zip-compressed"
        ],
        [".xlsx"] =
        [
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/octet-stream",
            "application/x-zip-compressed"
        ]
    };

    public static IReadOnlyCollection<string> AllowedExtensions => AllowedTypes.Keys;

    public static bool IsSupportedExtension(string? fileName) =>
        AllowedTypes.ContainsKey(Path.GetExtension(fileName ?? ""));

    public static IReadOnlyCollection<string>? GetAllowedContentTypes(string? fileName) =>
        AllowedTypes.TryGetValue(Path.GetExtension(fileName ?? ""), out var contentTypes)
            ? contentTypes
            : null;

    public static FileValidationResult Validate(IFormFileCollection files, FileUploadOptions options)
    {
        if (files.Count == 0)
        {
            return new FileValidationResult(false, "Vui lòng chọn ít nhất một file.");
        }

        long totalSize = 0;
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.FileName))
            {
                return new FileValidationResult(false, "Tên file không hợp lệ.");
            }

            if (!IsSupportedExtension(file.FileName))
            {
                return new FileValidationResult(false, $"File \"{file.FileName}\" không được hỗ trợ.");
            }

            if (file.Length == 0)
            {
                return new FileValidationResult(false, $"File \"{file.FileName}\" trống.");
            }

            if (file.Length > options.MaxFileSizeBytes)
            {
                return new FileValidationResult(
                    false,
                    $"File \"{file.FileName}\" vượt quá giới hạn {options.MaxFileSizeMb} MB.");
            }

            if (!HasValidContentType(file, file.FileName))
            {
                return new FileValidationResult(
                    false,
                    $"File \"{file.FileName}\" không phải file hợp lệ.");
            }

            if (!HasValidSignature(file))
            {
                return new FileValidationResult(
                    false,
                    $"File \"{file.FileName}\" không phải file hợp lệ.");
            }

            totalSize += file.Length;
            if (totalSize > options.MaxTotalSizeBytes)
            {
                return new FileValidationResult(
                    false,
                    $"Tổng dung lượng file vượt quá giới hạn {options.MaxTotalSizeMb} MB.");
            }
        }

        return new FileValidationResult(true);
    }

    private static bool HasValidContentType(IFormFile file, string fileName)
    {
        var allowedContentTypes = GetAllowedContentTypes(fileName);
        if (allowedContentTypes is null) return false;

        var actual = file.ContentType;
        if (string.IsNullOrWhiteSpace(actual)) return false;

        return allowedContentTypes.Contains(actual, StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasValidSignature(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var header = new byte[12];
            var read = 0;
            while (read < header.Length)
            {
                var n = stream.Read(header, read, header.Length - read);
                if (n == 0) break;
                read += n;
            }

            if (read < 4) return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => Matches(header, read, [0xFF, 0xD8, 0xFF]),
                ".png" => Matches(header, read, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
                ".pdf" => MatchesAscii(header, read, "%PDF"),
                ".webp" => MatchesAscii(header, read, "RIFF") && read >= 12 &&
                           header[8] == (byte)'W' && header[9] == (byte)'E' &&
                           header[10] == (byte)'B' && header[11] == (byte)'P',
                ".docx" or ".xlsx" => Matches(header, read, [0x50, 0x4B, 0x03, 0x04]),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private static bool MatchesAscii(byte[] header, int read, string ascii)
    {
        var bytes = Encoding.ASCII.GetBytes(ascii);
        return read >= bytes.Length && Matches(header, read, bytes);
    }

    private static bool Matches(byte[] header, int read, byte[] signature) =>
        read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature);
}