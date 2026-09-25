using System.Text.Json;
using FileGmail.Api.Options;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FileGmail.Api.Services;

public sealed class DbTokenDataStore : IDataStore, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);

    private readonly ILogger<DbTokenDataStore> _logger;
    private readonly string _databaseUrl;
    private NpgsqlDataSource? _dataSource;
    private bool _schemaReady;
    private bool _disabledReported;

    public DbTokenDataStore(IOptions<TokenStoreOptions> options, ILogger<DbTokenDataStore> logger)
    {
        _logger = logger;
        _databaseUrl = options.Value.DatabaseUrl.Trim();
    }

    public static bool TryValidateConnectionString(string connectionString, out string error)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            error = "DATABASE_URL (Supabase) chưa được cấu hình.";
            return false;
        }

        try
        {
            using var dataSource = NpgsqlDataSource.Create(connectionString);
            error = "";
            return true;
        }
        catch (Exception ex)
        {
            error = $"DATABASE_URL không hợp lệ: {ex.Message}";
            return false;
        }
    }

    public async Task ClearAsync()
    {
        if (await EnsureReadyAsync() is not { } dataSource)
        {
            return;
        }

        await ExecuteNonQueryAsync(dataSource, "DELETE FROM oauth_tokens;");
    }

    public async Task DeleteAsync<T>(string key)
    {
        if (await EnsureReadyAsync() is not { } dataSource)
        {
            return;
        }

        await ExecuteNonQueryAsync(dataSource, "DELETE FROM oauth_tokens WHERE key = @Key;", ("Key", key));
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (await EnsureReadyAsync() is not { } dataSource)
        {
            return default;
        }

        try
        {
            await using var cmd = dataSource.CreateCommand();
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
        catch (NpgsqlException ex)
        {
            _logger.LogWarning(ex, "Đọc token cho key '{Key}' từ Supabase thất bại", key);
            return default;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Token trong Supabase cho key '{Key}' bị hỏng", key);
            return default;
        }
    }

    public async Task StoreAsync<T>(string key, T value)
    {
        if (value is null)
        {
            return;
        }

        if (await EnsureReadyAsync() is not { } dataSource)
        {
            return;
        }

        var json = JsonSerializer.Serialize(value, SerializerOptions);
        await UpsertAsync(dataSource, key, json);
    }

    public void SeedFromJson(string json)
    {
        var values = TryParseTokenJson(json);
        if (values is null)
        {
            _logger.LogWarning("GMAIL_TOKEN_JSON không hợp lệ, bỏ qua seed.");
            return;
        }

        if (EnsureReadyAsync().GetAwaiter().GetResult() is not { } dataSource)
        {
            return;
        }

        foreach (var (key, value) in values)
        {
            SeedIfMissing(dataSource, key, value);
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

    private async Task<NpgsqlDataSource?> EnsureReadyAsync()
    {
        if (_dataSource is null)
        {
            if (!TryValidateConnectionString(_databaseUrl, out var error))
            {
                ReportDisabled(error);
                return null;
            }

            try
            {
                _dataSource = NpgsqlDataSource.Create(_databaseUrl);
            }
            catch (Exception ex)
            {
                ReportDisabled($"Không kết nối được Supabase: {ex.Message}");
                return null;
            }
        }

        if (!_schemaReady)
        {
            try
            {
                await using var cmd = _dataSource.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE IF NOT EXISTS oauth_tokens (
                        key TEXT PRIMARY KEY,
                        value TEXT NOT NULL,
                        updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
                    );
                    """;
                await cmd.ExecuteNonQueryAsync();
                _schemaReady = true;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogWarning(ex, "Supabase chưa sẵn sàng, thử lại sau.");
                return null;
            }
        }

        return _dataSource;
    }

    private void ReportDisabled(string error)
    {
        if (_disabledReported)
        {
            return;
        }

        _logger.LogError("TokenStore=db nhưng {Error}. Token sẽ KHÔNG được lưu vào Supabase.", error);
        _disabledReported = true;
    }

    private void SeedIfMissing(NpgsqlDataSource dataSource, string key, string value)
    {
        try
        {
            using var connection = dataSource.OpenConnection();
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

    private static async Task UpsertAsync(NpgsqlDataSource dataSource, string key, string json)
    {
        await using var cmd = dataSource.CreateCommand();
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

    private static async Task ExecuteNonQueryAsync(NpgsqlDataSource dataSource, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = dataSource.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }

        await cmd.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _dataSource?.Dispose();
    }
}