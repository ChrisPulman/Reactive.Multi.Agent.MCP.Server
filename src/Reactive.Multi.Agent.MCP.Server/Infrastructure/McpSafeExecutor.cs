using Reactive.Multi.Agent.MCP.Server.Serialization;

namespace Reactive.Multi.Agent.MCP.Server.Infrastructure;

internal static class McpSafeExecutor
{
    public static string ExecuteJson(string operationName, Func<object> action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            return JsonOutput.Serialize(action());
        }
        catch (Exception ex)
        {
            return JsonOutput.Serialize(new
            {
                ok = false,
                operation = operationName,
                error = new
                {
                    type = ex.GetType().Name,
                    message = ex.Message,
                },
                guidance = "The MCP server handled the failure and returned a safe error payload instead of terminating the host process.",
            });
        }
    }

    public static string ExecutePrompt(string operationName, Func<string> action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            return action();
        }
        catch (Exception ex)
        {
            return $"The MCP server could not complete '{operationName}'. Returned a safe prompt fallback instead of throwing. Error: {ex.GetType().Name}: {ex.Message}";
        }
    }
}
