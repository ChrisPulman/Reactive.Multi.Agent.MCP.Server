namespace Reactive.Multi.Agent.MCP.Core.Models;

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
