namespace FileStorage.Infrastructure.Redis;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Shared JSON settings for Redis payloads.</summary>
internal static class RedisJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}