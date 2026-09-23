using System.Net;
using System.Net.Http.Headers;
using FileGmail.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FileGmail.Tests;

public sealed class FileGmailApiTests : IClassFixture<FileGmailApiTests.Factory>
{
    private const string AccessKey = "secret-test";

    private readonly HttpClient _client;

    public FileGmailApiTests(Factory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"ok\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetStatus_ReturnsSenderRecipient()
    {
        var response = await _client.GetAsync("/api/gmail/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("sender", json);
        Assert.Contains("recipient", json);
    }

    [Fact]
    public async Task VerifyAccessKey_WithWrongKey_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/gmail/access-key/verify");
        request.Headers.Add("X-App-Key", "wrong-key");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VerifyAccessKey_WithCorrectKey_ReturnsOk()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/gmail/access-key/verify");
        request.Headers.Add("X-App-Key", AccessKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Send_WithoutAccessKey_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/gmail/send", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Send_WithUnsupportedFile_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x4d, 0x5a, 0x00, 0x00]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "files", "test-bad.exe");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/gmail/send") { Content = content };
        request.Headers.Add("X-App-Key", AccessKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        public Factory()
        {
            Environment.SetEnvironmentVariable("APP_ACCESS_KEY", AccessKey);
        }
    }
}