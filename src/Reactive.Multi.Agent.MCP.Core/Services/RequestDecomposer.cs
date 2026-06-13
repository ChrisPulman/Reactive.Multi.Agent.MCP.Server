using System.Text;

namespace Reactive.Multi.Agent.MCP.Core.Services;

/// <summary>
/// Provides functionality to decompose a high-level orchestration request into a structured execution plan with
/// agent-specific tasks and dependency-aware execution waves.
/// </summary>
/// <remarks>This class analyzes user requests, splits them into actionable clauses, and assigns each clause to
/// the most suitable agent profile based on routing keywords and preferences. The resulting plan organizes tasks into
/// execution waves that respect inter-task dependencies, enabling parallel and sequential execution as appropriate.
/// Thread safety is not guaranteed; create a new instance per orchestration if used concurrently.</remarks>
/// <param name="agentCatalog">The catalog of available agent profiles used to match and assign tasks during orchestration planning. Cannot be
/// null.</param>
public sealed class RequestDecomposer(IAgentCatalog agentCatalog) : IRequestDecomposer
{
    private readonly IReadOnlyList<AgentProfile> _agentProfiles = agentCatalog.GetAll();

    public OrchestrationPlan CreatePlan(OrchestrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var clauses = SplitClauses(request.UserRequest);
        var tasks = new List<AgentWorkItem>(clauses.Count);

        for (var index = 0; index < clauses.Count; index++)
        {
            var clause = clauses[index];
            var profile = SelectProfile(clause, request.PreferredAgents);
            var phase = DeterminePhase(profile);
            var taskId = $"task-{index + 1}";

            tasks.Add(new AgentWorkItem
            {
                TaskId = taskId,
                AgentId = profile.Id,
                AgentName = BuildAgentName(profile, taskId),
                AgentToolName = profile.ToolName,
                AgentSessionId = $"{profile.Id}-{taskId}",
                Title = BuildTitle(profile, clause),
                Objective = clause,
                ContextSnapshot = BuildContextSnapshot(request, clause, profile),
                PhaseName = phase.Name,
                PhaseOrder = phase.Order,
                SequenceOrder = index + 1,
                AcceptanceCriteria = BuildAcceptanceCriteria(profile, clause, request),
                SuggestedSkills = profile.DefaultSkills,
                SuggestedTools = profile.DefaultTools,
                Dependencies = [],
            });
        }

        ApplyDependencies(tasks);

        var executionWaves = tasks
            .GroupBy(task => new { task.PhaseOrder, task.PhaseName })
            .OrderBy(group => group.Key.PhaseOrder)
            .Select(group => new ExecutionWave
            {
                PhaseOrder = group.Key.PhaseOrder,
                PhaseName = group.Key.PhaseName,
                TaskIds = group.OrderBy(task => task.SequenceOrder).Select(task => task.TaskId).ToArray(),
            })
            .ToArray();

        return new OrchestrationPlan
        {
            Summary = BuildPlanSummary(request, tasks),
            ParallelizationWindow = Math.Min(request.MaxParallelAgents, Math.Max(tasks.Count, 1)),
            CoordinationNotes =
            [
                "Sessions and task updates are persisted in SQLite so orchestration state survives process restarts.",
                "Independent tasks in the same execution wave may be run in parallel by the MCP client.",
                "Later execution waves should wait for blocking dependencies from earlier waves.",
                "Record structured artifacts and handoff items through the specialist agent tools before finalizing the session.",
            ],
            ExecutionWaves = executionWaves,
            Tasks = tasks.OrderBy(task => task.PhaseOrder).ThenBy(task => task.SequenceOrder).ToArray(),
        };
    }

    private static IReadOnlyList<string> SplitClauses(string request)
    {
        var normalized = request
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace(";", ".", StringComparison.Ordinal)
            .Replace(", and ", ",", StringComparison.OrdinalIgnoreCase)
            .Replace(" and ", ",", StringComparison.OrdinalIgnoreCase)
            .Replace(", plus ", ",", StringComparison.OrdinalIgnoreCase)
            .Replace(" plus ", ",", StringComparison.OrdinalIgnoreCase);

        var rawSegments = normalized
            .Split(['.', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Trim())
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rawSegments.Count == 0)
        {
            rawSegments.Add(request.Trim());
        }

        return rawSegments;
    }

    private AgentProfile SelectProfile(string clause, IReadOnlyList<string> preferredAgents)
    {
        var scored = _agentProfiles
            .Select(profile => new
            {
                Profile = profile,
                Score = ScoreProfile(profile, clause, preferredAgents),
                Specificity = profile.RoutingKeywords.Max(static keyword => keyword.Length),
            })
            .OrderByDescending(entry => entry.Score)
            .ThenByDescending(entry => entry.Specificity)
            .ThenBy(entry => entry.Profile.Id, StringComparer.Ordinal)
            .ToList();

        if (scored.Count == 0)
        {
            throw new InvalidOperationException("No agent profiles are available.");
        }

        if (scored[0].Score > 0)
        {
            return scored[0].Profile;
        }

        return _agentProfiles.FirstOrDefault(profile => profile.Id.Equals("csharp", StringComparison.OrdinalIgnoreCase))
            ?? scored[0].Profile;
    }

    private static int ScoreProfile(AgentProfile profile, string clause, IReadOnlyList<string> preferredAgents)
    {
        var score = 0;

        foreach (var keyword in profile.RoutingKeywords)
        {
            if (!clause.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            score += keyword.Length >= 8 ? 12 : keyword.Length >= 5 ? 7 : 4;
        }


        if (preferredAgents.Any(agent => agent.Equals(profile.Id, StringComparison.OrdinalIgnoreCase)))
        {
            score += 10;
        }

        return score;
    }

    private static (string Name, int Order) DeterminePhase(AgentProfile profile)
        => profile.Category.ToLowerInvariant() switch
        {
            "strategy" => ("Strategy", 10),
            "implementation" => ("Implementation", 20),
            "automation" => ("Automation", 30),
            "documentation" => ("Documentation", 40),
            "validation" => ("Validation", 50),
            _ => ("Implementation", 20),
        };

    private static void ApplyDependencies(List<AgentWorkItem> tasks)
    {
        var orderedPhaseGroups = tasks
            .GroupBy(task => task.PhaseOrder)
            .OrderBy(group => group.Key)
            .ToList();

        for (var index = 1; index < orderedPhaseGroups.Count; index++)
        {
            var previousWave = orderedPhaseGroups[index - 1].OrderBy(task => task.SequenceOrder).ToList();
            var currentWave = orderedPhaseGroups[index].OrderBy(task => task.SequenceOrder).ToList();

            foreach (var task in currentWave)
            {
                task.Dependencies = previousWave
                    .Select(previous => new TaskDependency
                    {
                        TaskId = previous.TaskId,
                        Kind = DependencyKind.Blocking,
                        Reason = $"{task.PhaseName} work follows completion of the {previous.PhaseName} wave.",
                    })
                    .ToArray();
            }
        }
    }

    private static string BuildAgentName(AgentProfile profile, string taskId)
        => $"{profile.DisplayName} - {taskId}";

    private static string BuildTitle(AgentProfile profile, string clause)
        => $"{profile.DisplayName}: {char.ToUpperInvariant(clause[0])}{clause[1..]}";

    private static string BuildContextSnapshot(OrchestrationRequest request, string clause, AgentProfile profile)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Top-level request: {request.UserRequest}");
        builder.AppendLine($"Assigned slice: {clause}");
        builder.AppendLine($"Specialist domain: {profile.Domain}");
        builder.AppendLine($"Role boundary: {profile.Role}");

        if (request.Constraints.Count > 0)
        {
            builder.AppendLine($"Constraints: {string.Join(", ", request.Constraints)}");
        }

        if (request.DesiredArtifacts.Count > 0)
        {
            builder.AppendLine($"Desired artifacts: {string.Join(", ", request.DesiredArtifacts)}");
        }

        builder.AppendLine("Return structured artifacts, explicit risks, and handoff items for downstream agents.");
        return builder.ToString().Trim();
    }

    private static IReadOnlyList<string> BuildAcceptanceCriteria(AgentProfile profile, string clause, OrchestrationRequest request)
    {
        var criteria = new List<string>
        {
            $"Address the assigned objective: {clause}.",
            $"Stay inside the {profile.DisplayName} specialization: {profile.Role}.",
            "Return structured artifacts, explicit risks, and actionable handoff items.",
        };

        if (request.Constraints.Count > 0)
        {
            criteria.Add($"Honor constraints: {string.Join(", ", request.Constraints)}.");
        }

        if (request.DesiredArtifacts.Count > 0)
        {
            criteria.Add($"Prefer artifacts aligned with: {string.Join(", ", request.DesiredArtifacts)}.");
        }

        return criteria;
    }

    private static string BuildPlanSummary(OrchestrationRequest request, IReadOnlyList<AgentWorkItem> tasks)
        => $"Decomposed '{request.UserRequest}' into {tasks.Count} task(s) across {tasks.Select(task => task.AgentId).Distinct(StringComparer.OrdinalIgnoreCase).Count()} specialist agent type(s) with dependency-aware execution waves.";
}
