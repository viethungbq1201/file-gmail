using System.Text.Json;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Options;
using FileGmail.Api.Options;

namespace FileGmail.Api.Services;

public sealed class JsonTokenDataStore : IDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);

    private readonly ILogger<JsonTokenDataStore> _logger;
    private readonly Dictionary<string, string> _tokens = new(StringComparer.Ordinal);
    private readonly string? _filePath;
    private readonly object _sync = new();

    public JsonTokenDataStore(IOptions<GmailOptions> options, ILogger<JsonTokenDataStore> logger)
    {
        _logger = logger;

        var tokenPath = options.Value.TokenPath;
        if (!string.IsNullOrWhiteSpace(tokenPath))
        {
            _filePath = Path.GetFullPath(tokenPath);
            LoadFromFile();
        }

        var seedJson = options.Value.TokenJson;
        if (!string.IsNullOrWhiteSpace(seedJson))
        {
            SeedFromJson(seedJson);
        }
    }

    public Task ClearAsync()
    {
        lock (_sync)
        {
            _tokens.Clear();
        }

        if (_filePath is not null && File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync<T>(string key)
    {
        lock (_sync)
        {
            _tokens.Remove(key);
        }

        SaveToFile();
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        lock (_sync)
        {
            if (!_tokens.TryGetValue(key, out var json))
            {
                return Task.FromResult<T?>(default);
            }

            try
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(json, SerializerOptions));
            }
            catch (JsonException)
            {
                _logger.LogWarning("Token store entry for key '{Key}' is corrupted", key);
                return Task.FromResult<T?>(default);
            }
        }
    }

    public Task StoreAsync<T>(string key, T value)
    {
        if (value is null)
        {
            return Task.CompletedTask;
        }

        var json = JsonSerializer.Serialize(value, SerializerOptions);
        lock (_sync)
        {
            _tokens[key] = json;
        }

        SaveToFile();
        return Task.CompletedTask;
    }

    private void LoadFromFile()
    {
        if (_filePath is null || !File.Exists(_filePath)) return;

        try
        {
            var fileJson = File.ReadAllText(_filePath);
            SeedFromJson(fileJson);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Không đọc được token store từ {TokenPath}", _filePath);
        }
    }

    private void SeedFromJson(string json)
    {
        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json, SerializerOptions);
            if (values is null) return;

            lock (_sync)
            {
                foreach (var (key, value) in values)
                {
                    _tokens[key] = value;
                }
            }
        }
        catch (JsonException)
        {
            _logger.LogWarning("Token JSON không hợp lệ, bỏ qua.");
        }
    }

    private void SaveToFile()
    {
        if (_filePath is null) return;

        try
        {
            string json;
            lock (_sync)
            {
                json = JsonSerializer.Serialize(_tokens, SerializerOptions);
            }

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Không ghi được token store xuống {TokenPath}", _filePath);
        }
    }
}