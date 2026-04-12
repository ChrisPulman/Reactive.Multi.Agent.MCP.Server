namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a request to orchestrate an operation, including the user's intent, constraints, desired artifacts, and
/// agent preferences.
/// </summary>
/// <remarks>This class is used to encapsulate all information required to initiate an orchestration process. It
/// provides properties for specifying the user's request, any constraints to apply, the artifacts to produce, and
/// preferences for agent selection. The class is immutable after construction, except for property setters, and is
/// intended for use as a data transfer object in orchestration scenarios.</remarks>
public sealed class OrchestrationRequest
{
    public string UserRequest { get; set; } = string.Empty;

    public IReadOnlyList<string> Constraints { get; set; } = [];

    public IReadOnlyList<string> DesiredArtifacts { get; set; } = [];

    public IReadOnlyList<string> PreferredAgents { get; set; } = [];

    public int MaxParallelAgents { get; set; } = 4;

    public static OrchestrationRequest FromStrings(
        string userRequest,
        string? constraints = null,
        string? desiredArtifacts = null,
        string? preferredAgents = null,
        int maxParallelAgents = 4)
        => new()
        {
            UserRequest = userRequest,
            Constraints = SplitCsv(constraints),
            DesiredArtifacts = SplitCsv(desiredArtifacts),
            PreferredAgents = SplitCsv(preferredAgents),
            MaxParallelAgents = Math.Max(1, maxParallelAgents),
        };

    private static IReadOnlyList<string> SplitCsv(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
