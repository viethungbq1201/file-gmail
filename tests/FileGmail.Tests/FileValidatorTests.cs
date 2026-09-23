using FileGmail.Api.Options;
using FileGmail.Api.Validators;
using Microsoft.AspNetCore.Http;

namespace FileGmail.Tests;

public class FileValidatorTests
{
    private static readonly FileUploadOptions Options = new()
    {
        MaxFileSizeMb = 20,
        MaxTotalSizeMb = 25
    };

    [Fact]
    public void Validate_ValidJpeg_ReturnsValid()
    {
        var files = CreateFiles(("image.jpg", "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }));

        var result = FileValidator.Validate(files, Options);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_ValidPng_ReturnsValid()
    {
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        var files = CreateFiles(("anh.png", "image/png", pngSignature));

        var result = FileValidator.Validate(files, Options);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidPdf_ReturnsValid()
    {
        var files = CreateFiles(("document.pdf", "application/pdf", "%PDF-1.4"u8.ToArray()));

        var result = FileValidator.Validate(files, Options);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidWebp_ReturnsValid()
    {
        var webpHeader = new byte[12];
        "RIFF"u8.CopyTo(webpHeader);
        "WEBP"u8.CopyTo(webpHeader.AsSpan(8));
        var files = CreateFiles(("anh.webp", "image/webp", webpHeader));

        var result = FileValidator.Validate(files, Options);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_UnsupportedExtension_ReturnsError()
    {
        var files = CreateFiles(("abc.exe", "application/x-msdownload", "MZ"u8.ToArray()));

        var result = FileValidator.Validate(files, Options);

        Assert.False(result.IsValid);
        Assert.Contains("không được hỗ trợ", result.ErrorMessage);
    }

    [Fact]
    public void Validate_EmptyCollection_ReturnsError()
    {
        var result = FileValidator.Validate(new FormFileCollection(), Options);

        Assert.False(result.IsValid);
        Assert.Contains("ít nhất một file", result.ErrorMessage);
    }

    [Fact]
    public void Validate_OversizedSingleFile_ReturnsError()
    {
        var bigBytes = new byte[Options.MaxFileSizeBytes + 1];
        bigBytes[0] = 0xFF;
        bigBytes[1] = 0xD8;
        bigBytes[2] = 0xFF;
        var files = CreateFiles(("lon.jpg", "image/jpeg", bigBytes));

        var result = FileValidator.Validate(files, Options);

        Assert.False(result.IsValid);
        Assert.Contains("vượt quá giới hạn", result.ErrorMessage);
    }

    [Fact]
    public void Validate_InvalidContentType_ReturnsError()
    {
        var files = CreateFiles(("image.jpg", "text/plain", new byte[] { 0xFF, 0xD8, 0xFF }));

        var result = FileValidator.Validate(files, Options);

        Assert.False(result.IsValid);
        Assert.Contains("không phải file hợp lệ", result.ErrorMessage);
    }

    [Fact]
    public void Validate_WrongMagicBytes_ReturnsError()
    {
        var files = CreateFiles(("image.jpg", "image/jpeg", "%PDF-1.4"u8.ToArray()));

        var result = FileValidator.Validate(files, Options);

        Assert.False(result.IsValid);
        Assert.Contains("không phải file hợp lệ", result.ErrorMessage);
    }

    private static FormFileCollection CreateFiles(params (string Name, string Mime, byte[] Bytes)[] files)
    {
        var collection = new FormFileCollection();
        foreach (var (name, mime, bytes) in files)
        {
            var stream = new MemoryStream(bytes);
            var formFile = new FormFile(stream, 0, bytes.Length, "files", name)
            {
                Headers = new HeaderDictionary(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
                {
                    ["Content-Type"] = mime
                }),
                ContentType = mime
            };
            collection.Add(formFile);
        }

        return collection;
    }
}