using Reactive.Multi.Agent.MCP.Core.Models;

namespace Reactive.Multi.Agent.MCP.Core.Abstractions;

public interface IRequestDecomposer
{
    OrchestrationPlan CreatePlan(OrchestrationRequest request);
}
