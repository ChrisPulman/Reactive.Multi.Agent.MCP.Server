namespace Reactive.Multi.Agent.MCP.Core.Models;

public sealed class OrchestrationPlan
{
    public string Summary { get; set; } = string.Empty;

    public int ParallelizationWindow { get; set; }

    public IReadOnlyList<string> CoordinationNotes { get; set; } = [];

    public IReadOnlyList<ExecutionWave> ExecutionWaves { get; set; } = [];

    public IReadOnlyList<AgentWorkItem> Tasks { get; set; } = [];
}
