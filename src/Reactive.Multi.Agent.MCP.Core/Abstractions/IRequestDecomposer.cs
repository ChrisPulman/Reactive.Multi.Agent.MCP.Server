namespace Reactive.Multi.Agent.MCP.Core.Abstractions;

/// <summary>
/// Defines a contract for decomposing an orchestration request into an executable orchestration plan.
/// </summary>
/// <remarks>Implementations of this interface are responsible for analyzing the provided orchestration request
/// and generating a corresponding plan that can be executed by an orchestrator. This interface is typically used in
/// systems that require dynamic or configurable orchestration of operations based on incoming requests.</remarks>
public interface IRequestDecomposer
{
    /// <summary>
    /// Creates an orchestration plan based on the specified request parameters.
    /// </summary>
    /// <param name="request">The request containing the parameters and configuration for the orchestration plan. Cannot be null.</param>
    /// <returns>An instance of OrchestrationPlan that represents the plan generated from the provided request.</returns>
    OrchestrationPlan CreatePlan(OrchestrationRequest request);
}
