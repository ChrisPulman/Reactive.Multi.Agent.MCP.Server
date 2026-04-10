using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reactive.Multi.Agent.MCP.Server.Serialization;

public static class JsonOutput
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, Options);
}
