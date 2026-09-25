using System.Text.Json;
using FileGmail.Api.Options;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FileGmail.Api.Services;

public sealed class DbTokenDataStore : IDataStore, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);

    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DbTokenDataStore> _logger;

    public DbTokenDataStore(IOptions<TokenStoreOptions> options, ILogger<DbTokenDataStore> logger)
    {
        _logger = logger;
        _dataSource = NpgsqlDataSource.Create(options.Value.DatabaseUrl);
        EnsureSchema();
    }

    public async Task ClearAsync()
    {
        await ExecuteNonQueryAsync("DELETE FROM oauth_tokens;");
    }

    public async Task DeleteAsync<T>(string key)
    {
        await ExecuteNonQueryAsync("DELETE FROM oauth_tokens WHERE key = @Key;", ("Key", key));
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            await using var cmd = _dataSource.CreateCommand();
            cmd.CommandText = "SELECT value FROM oauth_tokens WHERE key = @Key LIMIT 1;";
            cmd.Parameters.AddWithValue("Key", key);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return default;
            }

            var json = reader.GetString(0);
            return JsonSerializer.Deserialize<T>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Token store entry cho key '{Key}' bị hỏng", key);
            return default;
        }
    }

    public async Task StoreAsync<T>(string key, T value)
    {
        if (value is null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(value, SerializerOptions);
        await UpsertAsync(key, json);
    }

    public void SeedFromJson(string json)
    {
        var values = TryParseTokenJson(json);
        if (values is null)
        {
            _logger.LogWarning("GMAIL_TOKEN_JSON không hợp lệ, bỏ qua seed.");
            return;
        }

        foreach (var (key, value) in values)
        {
            SeedIfMissing(key, value);
        }
    }

    public static Dictionary<string, string>? TryParseTokenJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void EnsureSchema()
    {
        using var connection = _dataSource.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS oauth_tokens (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );
            """;
        command.ExecuteNonQuery();
    }

    private void SeedIfMissing(string key, string value)
    {
        try
        {
            using var connection = _dataSource.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO oauth_tokens (key, value)
                VALUES (@Key, @Value)
                ON CONFLICT (key) DO NOTHING;
                """;
            command.Parameters.AddWithValue("Key", key);
            command.Parameters.AddWithValue("Value", value);
            command.ExecuteNonQuery();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogWarning(ex, "Không seed được token cho key '{Key}'", key);
        }
    }

    private async Task UpsertAsync(string key, string json)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = """
            INSERT INTO oauth_tokens (key, value, updated_at)
            VALUES (@Key, @Value, now())
            ON CONFLICT (key) DO UPDATE
                SET value = EXCLUDED.value,
                    updated_at = now();
            """;
        cmd.Parameters.AddWithValue("Key", key);
        cmd.Parameters.AddWithValue("Value", json);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ExecuteNonQueryAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = _dataSource.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }

        await cmd.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _dataSource.Dispose();
    }
}