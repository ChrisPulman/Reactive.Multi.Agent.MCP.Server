namespace Reactive.Multi.Agent.MCP.Core.Models;

/// <summary>
/// Represents a single execution phase within a multi-phase process, including its order, name, and associated task
/// identifiers.
/// </summary>
public sealed class ExecutionWave
{
    public int PhaseOrder { get; set; }

    public string PhaseName { get; set; } = string.Empty;

    public IReadOnlyList<string> TaskIds { get; set; } = [];
}
