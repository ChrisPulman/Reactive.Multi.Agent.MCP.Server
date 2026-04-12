namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a plan for orchestrating the execution of tasks across multiple agents and execution waves.
/// </summary>
/// <remarks>An orchestration plan defines the structure and coordination details for executing a set of tasks,
/// including parallelization settings and execution order. This type is typically used to coordinate complex workflows
/// that require multiple stages or waves of execution, potentially with dependencies or coordination notes between
/// them.</remarks>
public sealed class OrchestrationPlan
{
    public string Summary { get; set; } = string.Empty;

    public int ParallelizationWindow { get; set; }

    public IReadOnlyList<string> CoordinationNotes { get; set; } = [];

    public IReadOnlyList<ExecutionWave> ExecutionWaves { get; set; } = [];

    public IReadOnlyList<AgentWorkItem> Tasks { get; set; } = [];
}
