namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class SupervisorActionPlan
{
    public string SessionId { get; set; } = string.Empty;

    public DateTimeOffset EvaluatedAtUtc { get; set; }

    public IReadOnlyList<string> OrderedActions { get; set; } = [];

    public IReadOnlyList<string> AutoAppliedActions { get; set; } = [];

    public IReadOnlyList<NextTaskCandidate> NextRunnableTasks { get; set; } = [];

    public IReadOnlyList<string> ActionIds { get; set; } = [];
}
