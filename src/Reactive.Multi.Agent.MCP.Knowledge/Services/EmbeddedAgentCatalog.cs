using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Models;
using System.Reflection;
using System.Text.Json;

namespace Reactive.Multi.Agent.MCP.Knowledge.Services;

/// <summary>
/// Provides access to a catalog of agent profiles that are embedded as resources within the assembly.
/// </summary>
/// <remarks>The catalog loads agent profiles from embedded JSON resources at runtime. All profile data is
/// read-only and remains consistent for the lifetime of the application. This implementation is thread-safe and
/// suitable for scenarios where agent definitions are bundled with the application.</remarks>
public sealed class EmbeddedAgentCatalog : IAgentCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Lazy<IReadOnlyList<AgentProfile>> Profiles = new(LoadProfiles);

    public IReadOnlyList<AgentProfile> GetAll() => Profiles.Value;

    public AgentProfile? GetById(string id)
        => Profiles.Value.FirstOrDefault(profile => profile.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<AgentProfile> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetAll();
        }

        return
        [
            .. Profiles.Value
                .Where(profile =>
                    profile.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || profile.Domain.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || profile.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || profile.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || profile.Summary.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || profile.Role.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || profile.DefaultSkills.Any(skill => skill.Contains(query, StringComparison.OrdinalIgnoreCase))
                    || profile.DefaultTools.Any(tool => tool.Contains(query, StringComparison.OrdinalIgnoreCase))
                    || profile.RoutingKeywords.Any(keyword => keyword.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(profile => profile.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static List<AgentProfile> LoadProfiles()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Catalog.", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var profiles = new List<AgentProfile>(resourceNames.Length);
        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var profile = JsonSerializer.Deserialize<AgentProfile>(json, SerializerOptions)
                ?? throw new InvalidOperationException($"Embedded agent profile '{resourceName}' could not be deserialized.");
            profiles.Add(profile);
        }

        return profiles;
    }
}
