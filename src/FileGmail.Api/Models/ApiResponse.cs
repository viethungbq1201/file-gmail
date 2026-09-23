using System.Text.Json.Serialization;

namespace FileGmail.Api.Models;

public record ApiResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string? Message = null,
    [property: JsonPropertyName("data")] object? Data = null);