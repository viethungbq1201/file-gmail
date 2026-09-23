using System.Text;
using FileGmail.Api.Services;

namespace FileGmail.Tests;

public class EmailMessageBuilderTests
{
    [Fact]
    public void Build_SetsFromToAndSubject()
    {
        var raw = EmailMessageBuilder.Build(
            "sender@gmail.com", "recipient@gmail.com", "Tiêu đề kiểm tra", "Nội dung",
            []);

        Assert.Contains("To: recipient@gmail.com\r\n", raw);
        Assert.Contains("From: sender@gmail.com\r\n", raw);
        Assert.StartsWith("MIME-Version: 1.0", raw);
        Assert.Contains("Subject: =?UTF-8?B?", raw);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("Tiêu đề kiểm tra"));
        Assert.Contains($"Subject: =?UTF-8?B?{encoded}?=", raw);
    }

    [Fact]
    public void Build_SetsMultipartMixed()
    {
        var raw = EmailMessageBuilder.Build("a@x.com", "b@x.com", "s", "b", []);

        Assert.Contains("Content-Type: multipart/mixed; boundary=\"", raw);
        Assert.Contains("Content-Type: text/plain; charset=UTF-8\r\n", raw);
    }

    [Fact]
    public void Build_IncludesBodyInBase64()
    {
        var raw = EmailMessageBuilder.Build("a@x.com", "b@x.com", "s", "Nội dung email", []);

        Assert.Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes("Nội dung email")), raw);
    }

    [Fact]
    public void Build_SingleAttachment_IncludesItsHeadersAndData()
    {
        var fileBytes = new byte[] { 1, 2, 3, 4, 5 };
        var attachment = new AttachmentData("document.pdf", "application/pdf", fileBytes);

        var raw = EmailMessageBuilder.Build("a@x.com", "b@x.com", "s", "b", [attachment]);

        Assert.Contains("Content-Type: application/pdf\r\n", raw);
        Assert.Contains("Content-Disposition: attachment; filename*=UTF-8''document.pdf", raw);
        Assert.Contains(Convert.ToBase64String(fileBytes), raw);
    }

    [Fact]
    public void Build_MultipleAttachments_IncludesAll()
    {
        var attachments = new[]
        {
            new AttachmentData("a.pdf", "application/pdf", new byte[] { 1 }),
            new AttachmentData("b.png", "image/png", new byte[] { 2 }),
            new AttachmentData("c.jpg", "image/jpeg", new byte[] { 3 })
        };

        var raw = EmailMessageBuilder.Build("a@x.com", "b@x.com", "s", "b", attachments);

        Assert.Contains("filename*=UTF-8''a.pdf", raw);
        Assert.Contains("filename*=UTF-8''b.png", raw);
        Assert.Contains("filename*=UTF-8''c.jpg", raw);
        Assert.Contains(Convert.ToBase64String(new byte[] { 1 }), raw);
        Assert.Contains(Convert.ToBase64String(new byte[] { 2 }), raw);
        Assert.Contains(Convert.ToBase64String(new byte[] { 3 }), raw);
    }

    [Fact]
    public void Build_EndsWithClosingBoundary()
    {
        var raw = EmailMessageBuilder.Build("a@x.com", "b@x.com", "s", "b", []);

        var boundaryMatch = System.Text.RegularExpressions.Regex.Match(raw, "boundary=\"([^\"]+)\"");
        Assert.True(boundaryMatch.Success);
        Assert.EndsWith($"--{boundaryMatch.Groups[1].Value}--\r\n", raw);
    }

    [Fact]
    public void ToBase64Url_ProducesBase64UrlVariant()
    {
        var value = "hello world";

        var result = EmailMessageBuilder.ToBase64Url(value);

        Assert.Equal("aGVsbG8gd29ybGQ", result);
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("=", result);
    }
}