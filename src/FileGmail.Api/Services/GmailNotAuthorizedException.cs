namespace FileGmail.Api.Services;

public sealed class GmailNotAuthorizedException : InvalidOperationException
{
    public GmailNotAuthorizedException(string message)
        : base(message)
    {
    }

    public GmailNotAuthorizedException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}