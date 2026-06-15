using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace Reactive.Multi.Agent.MCP.Tests;

public class McpServerIntegrationTests
{
    private static McpClient _client = null!;

    [Before(Class)]
    public static async Task SetUpClientAsync(ClassHookContext _)
    {
        var stateDir = Path.Combine(Path.GetTempPath(), "mcp-integration-" + Guid.NewGuid().ToString("N"));
        var serverCsproj = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../Reactive.Multi.Agent.MCP.Server/CP.Reactive.Multi.Agent.MCP.Server.csproj"));

        var configuration = AppContext.BaseDirectory.Contains("Release", StringComparison.OrdinalIgnoreCase)
            ? "Release" : "Debug";

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = "dotnet",
            Arguments = new List<string> { "run", "--no-build", "--configuration", configuration, "--project", serverCsproj },
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["REACTIVE_MULTI_AGENT_MCP_STATE_ROOT"] = stateDir,
            },
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        _client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
    }

    [After(Class)]
    public static async Task TearDownClientAsync(ClassHookContext _)
    {
        if (_client is not null)
            await _client.DisposeAsync();
    }

    [Test]
    [Timeout(30_000)]
    public async Task Server_Lists_33_Registered_Tools(CancellationToken cancellationToken)
    {
        var tools = await _client.ListToolsAsync(cancellationToken: cancellationToken);

        await Assert.That(tools.Count).IsEqualTo(33);
    }

    [Test]
    [Timeout(30_000)]
    public async Task Server_Tool_Names_Include_All_Expected_Categories(CancellationToken cancellationToken)
    {
        var tools = await _client.ListToolsAsync(cancellationToken: cancellationToken);
        var names = tools.Select(t => t.Name).ToHashSet();

        await Assert.That(names).Contains("multiagent_orchestrate_request");
        await Assert.That(names).Contains("multiagent_csharp_agent");
        await Assert.That(names).Contains("multiagent_blazor_agent");
        await Assert.That(names).Contains("multiagent_agent_catalog_list");
        await Assert.That(names).Contains("multiagent_finalize_session");
        await Assert.That(names).Contains("multiagent_supervisor_status");
    }

    [Test]
    [Timeout(30_000)]
    public async Task OrchestrateRequest_Tool_Is_Discoverable_With_Required_First_Call_Description(CancellationToken cancellationToken)
    {
        var tools = await _client.ListToolsAsync(cancellationToken: cancellationToken);
        var tool = tools.Single(candidate => candidate.Name == "multiagent_orchestrate_request");

        await Assert.That(tool.Description).Contains("Create a new durable orchestration session");
        await Assert.That(tool.Description).Contains("Call this before any specialist worker agent tool");
    }

    [Test]
    [Timeout(30_000)]
    public async Task Server_Lists_4_Static_Resources(CancellationToken cancellationToken)
    {
        var resources = await _client.ListResourcesAsync(cancellationToken: cancellationToken);

        await Assert.That(resources.Count).IsEqualTo(4);
    }

    [Test]
    [Timeout(30_000)]
    public async Task Server_Lists_1_Resource_Template(CancellationToken cancellationToken)
    {
        var templates = await _client.ListResourceTemplatesAsync(cancellationToken: cancellationToken);

        await Assert.That(templates.Count).IsEqualTo(1);
        await Assert.That(templates[0].UriTemplate).Contains("session");
    }

    [Test]
    [Timeout(30_000)]
    public async Task Server_Lists_3_Registered_Prompts(CancellationToken cancellationToken)
    {
        var prompts = await _client.ListPromptsAsync(cancellationToken: cancellationToken);

        await Assert.That(prompts.Count).IsEqualTo(3);
    }

    [Test]
    [Timeout(30_000)]
    public async Task Server_Prompt_Names_Include_All_Expected_Prompts(CancellationToken cancellationToken)
    {
        var prompts = await _client.ListPromptsAsync(cancellationToken: cancellationToken);
        var names = prompts.Select(p => p.Name).ToHashSet();

        await Assert.That(names).Contains("create_multi_agent_plan");
        await Assert.That(names).Contains("create_specialist_agent_prompt");
        await Assert.That(names).Contains("merge_multi_agent_results");
    }

    [Test]
    [Timeout(30_000)]
    public async Task OrchestrateRequest_Tool_Returns_Session_Payload(CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            "multiagent_orchestrate_request",
            new Dictionary<string, object?> { ["userRequest"] = "Build a C# console app" },
            cancellationToken: cancellationToken);

        await Assert.That(result.IsError ?? false).IsFalse();
        var text = ((TextContentBlock)result.Content[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("sessionId", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("progress", out var progress)).IsTrue();
        await Assert.That(progress.TryGetProperty("status", out _)).IsTrue();
        await Assert.That(progress.TryGetProperty("totalTasks", out _)).IsTrue();
    }

    [Test]
    [Timeout(30_000)]
    public async Task CsharpAgent_With_Invalid_Session_Returns_Safe_Error_Payload(CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            "multiagent_csharp_agent",
            new Dictionary<string, object?> { ["sessionId"] = "invalid-session-000", ["taskId"] = "task-1" },
            cancellationToken: cancellationToken);

        await Assert.That(result.IsError ?? false).IsFalse();
        var text = ((TextContentBlock)result.Content[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("ok", out var ok)).IsTrue();
        await Assert.That(ok.GetBoolean()).IsFalse();
        await Assert.That(root.TryGetProperty("error", out _)).IsTrue();
    }

    [Test]
    [Timeout(30_000)]
    public async Task CatalogList_Tool_Returns_Agents_Count_And_Array(CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            "multiagent_agent_catalog_list",
            new Dictionary<string, object?>(),
            cancellationToken: cancellationToken);

        await Assert.That(result.IsError ?? false).IsFalse();
        var text = ((TextContentBlock)result.Content[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("count", out var count)).IsTrue();
        await Assert.That(count.GetInt32()).IsGreaterThanOrEqualTo(15);
        await Assert.That(root.TryGetProperty("agents", out _)).IsTrue();
    }

    [Test]
    [Timeout(30_000)]
    public async Task Catalog_Resource_Read_Returns_Agents_Json(CancellationToken cancellationToken)
    {
        var result = await _client.ReadResourceAsync(
            "multiagent://catalog",
            cancellationToken: cancellationToken);

        await Assert.That(result.Contents.Count).IsGreaterThan(0);
        var text = ((TextResourceContents)result.Contents[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("count", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("agents", out _)).IsTrue();
    }

    [Test]
    [Timeout(30_000)]
    public async Task RecentHistory_Resource_Read_Returns_Json_Array(CancellationToken cancellationToken)
    {
        var result = await _client.ReadResourceAsync(
            "multiagent://history/recent",
            cancellationToken: cancellationToken);

        await Assert.That(result.Contents.Count).IsGreaterThan(0);
        var text = ((TextResourceContents)result.Contents[0]).Text;
        using var doc = JsonDocument.Parse(text);
        await Assert.That(doc.RootElement.ValueKind).IsEqualTo(JsonValueKind.Array);
    }

    [Test]
    [Timeout(30_000)]
    public async Task Architecture_Resource_Read_Returns_Hub_And_Spoke_Model(CancellationToken cancellationToken)
    {
        var result = await _client.ReadResourceAsync(
            "multiagent://architecture/hub-and-spoke",
            cancellationToken: cancellationToken);

        await Assert.That(result.Contents.Count).IsGreaterThan(0);
        var text = ((TextResourceContents)result.Contents[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("model", out var model)).IsTrue();
        await Assert.That(model.GetString()).IsEqualTo("hub-and-spoke");
    }

    [Test]
    [Timeout(30_000)]
    public async Task ArtifactSchema_Resource_Read_Returns_Example_Schema(CancellationToken cancellationToken)
    {
        var result = await _client.ReadResourceAsync(
            "multiagent://schemas/artifacts",
            cancellationToken: cancellationToken);

        await Assert.That(result.Contents.Count).IsGreaterThan(0);
        var text = ((TextResourceContents)result.Contents[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("artifacts", out _)).IsTrue();
        await Assert.That(root.TryGetProperty("handoffItems", out _)).IsTrue();
    }

    [Test]
    [Timeout(30_000)]
    public async Task CreateMultiAgentPlan_Prompt_Returns_Phase_Guide(CancellationToken cancellationToken)
    {
        var result = await _client.GetPromptAsync(
            "create_multi_agent_plan",
            new Dictionary<string, object?> { ["userRequest"] = "Build a Blazor app with CI" },
            cancellationToken: cancellationToken);

        await Assert.That(result.Messages.Count).IsGreaterThan(0);
        var messageText = ((TextContentBlock)result.Messages[0].Content).Text;
        await Assert.That(messageText).Contains("multiagent_orchestrate_request");
        await Assert.That(messageText).Contains("Phase");
    }

    // ── Blank required parameter tests ───────────────────────────────────────
    // Verify that omitting a required string parameter returns a safe error payload
    // (ok=false, error.message names the parameter) rather than an MCP-level error.

    [Test]
    [Timeout(30_000)]
    public async Task OrchestrateRequest_With_Blank_UserRequest_Returns_Safe_Error_Naming_Parameter(CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            "multiagent_orchestrate_request",
            new Dictionary<string, object?> { ["userRequest"] = "" },
            cancellationToken: cancellationToken);

        await Assert.That(result.IsError ?? false).IsFalse();
        var text = ((TextContentBlock)result.Content[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("ok", out var ok)).IsTrue();
        await Assert.That(ok.GetBoolean()).IsFalse();
        var message = root.GetProperty("error").GetProperty("message").GetString();
        await Assert.That(message).Contains("userRequest");
    }

    [Test]
    [Timeout(30_000)]
    public async Task SessionStatus_With_Blank_SessionId_Returns_Safe_Error_Naming_Parameter(CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            "multiagent_session_status",
            new Dictionary<string, object?> { ["sessionId"] = "" },
            cancellationToken: cancellationToken);

        await Assert.That(result.IsError ?? false).IsFalse();
        var text = ((TextContentBlock)result.Content[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("ok", out var ok)).IsTrue();
        await Assert.That(ok.GetBoolean()).IsFalse();
        var message = root.GetProperty("error").GetProperty("message").GetString();
        await Assert.That(message).Contains("sessionId");
    }

    [Test]
    [Timeout(30_000)]
    public async Task CSharpAgent_With_Blank_SessionId_Returns_Safe_Error_Naming_Parameter(CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            "multiagent_csharp_agent",
            new Dictionary<string, object?> { ["sessionId"] = "", ["taskId"] = "task-1" },
            cancellationToken: cancellationToken);

        await Assert.That(result.IsError ?? false).IsFalse();
        var text = ((TextContentBlock)result.Content[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("ok", out var ok)).IsTrue();
        await Assert.That(ok.GetBoolean()).IsFalse();
        var message = root.GetProperty("error").GetProperty("message").GetString();
        await Assert.That(message).Contains("sessionId");
    }

    [Test]
    [Timeout(30_000)]
    public async Task CSharpAgent_With_Blank_TaskId_Returns_Safe_Error_Naming_Parameter(CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            "multiagent_csharp_agent",
            new Dictionary<string, object?> { ["sessionId"] = "some-session", ["taskId"] = "" },
            cancellationToken: cancellationToken);

        await Assert.That(result.IsError ?? false).IsFalse();
        var text = ((TextContentBlock)result.Content[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("ok", out var ok)).IsTrue();
        await Assert.That(ok.GetBoolean()).IsFalse();
        var message = root.GetProperty("error").GetProperty("message").GetString();
        await Assert.That(message).Contains("taskId");
    }

    [Test]
    [Timeout(30_000)]
    public async Task GetAgent_With_Blank_Id_Returns_Safe_Error_Naming_Parameter(CancellationToken cancellationToken)
    {
        var result = await _client.CallToolAsync(
            "multiagent_agent_catalog_get",
            new Dictionary<string, object?> { ["id"] = "" },
            cancellationToken: cancellationToken);

        await Assert.That(result.IsError ?? false).IsFalse();
        var text = ((TextContentBlock)result.Content[0]).Text;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        await Assert.That(root.TryGetProperty("ok", out var ok)).IsTrue();
        await Assert.That(ok.GetBoolean()).IsFalse();
        var message = root.GetProperty("error").GetProperty("message").GetString();
        await Assert.That(message).Contains("id");
    }
}
