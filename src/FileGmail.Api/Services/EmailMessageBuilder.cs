using System.Text;

namespace FileGmail.Api.Services;

public sealed record AttachmentData(string FileName, string ContentType, ReadOnlyMemory<byte> Content);

public static class EmailMessageBuilder
{
    private const int Base64LineLength = 76;

    public static string Build(
        string from,
        string to,
        string subject,
        string body,
        IEnumerable<AttachmentData> attachments,
        CancellationToken cancellationToken = default)
    {
        var boundary = $"=_FileGmail_{Guid.NewGuid():N}";
        var builder = new StringBuilder(4096);

        builder.Append("MIME-Version: 1.0\r\n");
        builder.Append($"To: {to}\r\n");
        builder.Append($"From: {from}\r\n");
        builder.Append($"Subject: {EncodeHeader(subject)}\r\n");
        builder.Append($"Content-Type: multipart/mixed; boundary=\"{boundary}\"\r\n\r\n");

        builder.Append($"--{boundary}\r\n");
        builder.Append("Content-Type: text/plain; charset=UTF-8\r\n");
        builder.Append("Content-Transfer-Encoding: base64\r\n\r\n");
        builder.Append(WrapBase64(Convert.ToBase64String(Encoding.UTF8.GetBytes(body))));
        builder.Append("\r\n\r\n");

        foreach (var attachment in attachments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            builder.Append($"--{boundary}\r\n");
            builder.Append($"Content-Type: {attachment.ContentType}\r\n");
            builder.Append("Content-Disposition: attachment; filename*=UTF-8''");
            builder.Append(Uri.EscapeDataString(attachment.FileName));
            builder.Append("\r\n");
            builder.Append("Content-Transfer-Encoding: base64\r\n\r\n");
            builder.Append(WrapBase64(Convert.ToBase64String(attachment.Content.Span)));
            builder.Append("\r\n\r\n");
        }

        builder.Append($"--{boundary}--\r\n");
        return builder.ToString();
    }

    public static string ToBase64Url(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string EncodeHeader(string value)
    {
        return value.All(static c => c < 128)
            ? value
            : $"=?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}?=";
    }

    private static string WrapBase64(string base64)
    {
        if (base64.Length <= Base64LineLength) return base64;

        var lineCount = (base64.Length + Base64LineLength - 1) / Base64LineLength;
        var lines = new string[lineCount];
        for (var i = 0; i < lineCount; i++)
        {
            var offset = i * Base64LineLength;
            lines[i] = base64.Substring(offset, Math.Min(Base64LineLength, base64.Length - offset));
        }

        return string.Join("\r\n", lines);
    }
}