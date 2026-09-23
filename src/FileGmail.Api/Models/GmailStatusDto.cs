namespace FileGmail.Api.Models;

public sealed record GmailStatusDto(
    bool Authorized,
    string Sender,
    string Recipient);