using System.Text.Json;
using FileGmail.Api.Services;

namespace FileGmail.Tests;

public class DbTokenDataStoreTests
{
    [Fact]
    public void TryParseTokenJson_ValidSeed_ReturnsDictionary()
    {
        var json = """{"me":"{\"AccessToken\":\"abc\",\"ExpiresInSeconds\":3599}"}""";

        var result = DbTokenDataStore.TryParseTokenJson(json);

        Assert.NotNull(result);
        Assert.True(result.ContainsKey("me"));
        Assert.Contains("abc", result["me"]);
    }

    [Fact]
    public void TryParseTokenJson_EmptyOrInvalid_ReturnsNull()
    {
        Assert.Null(DbTokenDataStore.TryParseTokenJson(""));
        Assert.Null(DbTokenDataStore.TryParseTokenJson("khong-phai-json"));
    }

    [Fact]
    public void TryParseTokenJson_RealTokenShape_KeepsNestedEscaping()
    {
        var token = new
        {
            AccessToken = "ya29.abc",
            TokenType = "Bearer",
            ExpiresInSeconds = 3599,
            RefreshToken = "1//0refresh"
        };
        var nested = JsonSerializer.Serialize(token);
        var seed = JsonSerializer.Serialize(new Dictionary<string, string> { ["me"] = nested });

        var result = DbTokenDataStore.TryParseTokenJson(seed);

        Assert.NotNull(result);
        Assert.Equal(nested, result["me"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("khong-phai-connection-string")]
    public void TryValidateConnectionString_Invalid_ReturnsFalse(string connectionString)
    {
        Assert.False(DbTokenDataStore.TryValidateConnectionString(connectionString!, out _));
    }

    [Fact]
    public void TryValidateConnectionString_DsnLike_ReturnsTrue()
    {
        var valid = "Host=aws-0-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.abc;Password=secret;SSL Mode=Require;Trust Server Certificate=true";

        Assert.True(DbTokenDataStore.TryValidateConnectionString(valid, out _));
    }

    [Fact]
    public void TryValidateConnectionString_SupabaseUri_ReturnsTrue()
    {
        const string uri = "postgresql://postgres.epfdlrnpokvmiuasafug:secret@aws-0-ap-northeast-2.pooler.supabase.com:5432/postgres";

        Assert.True(DbTokenDataStore.TryValidateConnectionString(uri, out _));
    }
}