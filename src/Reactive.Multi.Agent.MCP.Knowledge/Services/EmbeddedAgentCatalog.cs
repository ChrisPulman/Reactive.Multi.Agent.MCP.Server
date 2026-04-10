using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Models;
using System.Reflection;
using System.Text.Json;

namespace Reactive.Multi.Agent.MCP.Knowledge.Services;

public sealed class EmbeddedAgentCatalog : IAgentCatalog
{
    private static readonly Lazy<IReadOnlyList<AgentProfile>> Profiles = new(LoadProfiles);

    public IReadOnlyList<AgentProfile> GetAll() => Profiles.Value;

    public AgentProfile? GetById(string id)
        => Profiles.Value.FirstOrDefault(profile => profile.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<AgentProfile> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return this.GetAll();
        }

        var lowered = query.ToLowerInvariant();
        return Profiles.Value
            .Where(profile =>
                profile.Id.Contains(lowered, StringComparison.OrdinalIgnoreCase)
                || profile.Domain.Contains(lowered, StringComparison.OrdinalIgnoreCase)
                || profile.Category.Contains(lowered, StringComparison.OrdinalIgnoreCase)
                || profile.DisplayName.Contains(lowered, StringComparison.OrdinalIgnoreCase)
                || profile.Summary.Contains(lowered, StringComparison.OrdinalIgnoreCase)
                || profile.Role.Contains(lowered, StringComparison.OrdinalIgnoreCase)
                || profile.DefaultSkills.Any(skill => skill.Contains(lowered, StringComparison.OrdinalIgnoreCase))
                || profile.DefaultTools.Any(tool => tool.Contains(lowered, StringComparison.OrdinalIgnoreCase))
                || profile.RoutingKeywords.Any(keyword => keyword.Contains(lowered, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(profile => profile.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<AgentProfile> LoadProfiles()
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
            var profile = JsonSerializer.Deserialize<AgentProfile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }) ?? throw new InvalidOperationException($"Embedded agent profile '{resourceName}' could not be deserialized.");
            profiles.Add(profile);
        }

        return profiles;
    }
}
