using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reactive.Multi.Agent.MCP.Server.Serialization;

/// <summary>
/// Provides methods for serializing objects to JSON using predefined serialization options.
/// </summary>
/// <remarks>This class uses a consistent set of serialization options, including camel case property naming,
/// ignoring null values, and serializing enums as strings. The options are suitable for most common scenarios where
/// compact, camel-cased JSON output is desired.</remarks>
public static class JsonOutput
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, Options);
}
