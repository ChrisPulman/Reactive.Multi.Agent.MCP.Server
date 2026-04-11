using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Core.Configuration;
using Reactive.Multi.Agent.MCP.Core.Persistence;
using Reactive.Multi.Agent.MCP.Core.Services;
using Reactive.Multi.Agent.MCP.Knowledge.Services;
using Reactive.Multi.Agent.MCP.Server.Prompts;
using Reactive.Multi.Agent.MCP.Server.Resources;
using Reactive.Multi.Agent.MCP.Server.Tools;
using System.Diagnostics;

namespace Reactive.Multi.Agent.MCP.Server;

public static class Program
{
    public static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

        builder.Services.AddSingleton(CreateOptions());
        builder.Services.AddSingleton<IAgentCatalog, EmbeddedAgentCatalog>();
        builder.Services.AddSingleton<IRequestDecomposer, RequestDecomposer>();
        builder.Services.AddSingleton<IOrchestrationSessionStore, SqliteOrchestrationSessionStore>();
        builder.Services.AddSingleton<IOrchestrationService, OrchestrationService>();

        builder.Services.AddMcpServer(options => options.ServerInfo = new Implementation
        {
            Name = "reactive-multi-agent-mcp-server",
            Version = typeof(Program).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()?.InformationalVersion
                ?? typeof(Program).Assembly.GetName().Version?.ToString()
                ?? "0.0.0",
            Title = "Reactive Multi Agent MCP Server",
            Description = "Hub-and-spoke orchestration server for multi-agent task decomposition, durable session state, dependency-aware execution waves, and structured artifact merging inside Copilot Chat.",
            WebsiteUrl = "https://github.com/ChrisPulman/Reactive.Multi.Agent.MCP.Server",
            Icons =
            [
                new Icon
                {
                    Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Robot/Flat/robot_flat.svg",
                    MimeType = "image/svg+xml",
                    Sizes = ["any"],
                    Theme = "light",
                },
                new Icon
                {
                    Source = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Robot/3D/robot_3d.png",
                    MimeType = "image/png",
                    Sizes = ["256x256"],
                },
            ],
        })
            .WithStdioServerTransport()
            .WithTools<AgentCatalogTools>()
            .WithTools<OrchestratorTools>()
            .WithTools<WorkerAgentTools>()
            .WithResources<OrchestrationResources>()
            .WithPrompts<OrchestrationPrompts>();

        return builder.Build();
    }

    public static ReactiveMultiAgentOptions CreateOptions()
    {
        var stateRoot = Environment.GetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_STATE_ROOT");
        var packageId = Environment.GetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_PACKAGE_ID");
        var serverId = Environment.GetEnvironmentVariable("REACTIVE_MULTI_AGENT_MCP_SERVER_ID");

        return new ReactiveMultiAgentOptions
        {
            StateRootPath = string.IsNullOrWhiteSpace(stateRoot)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReactiveMultiAgentMcp")
                : stateRoot,
            PackageId = string.IsNullOrWhiteSpace(packageId)
                ? "CP.Reactive.Multi.Agent.MCP.Server"
                : packageId,
            ServerId = string.IsNullOrWhiteSpace(serverId)
                ? "io.github.chrispulman/reactive-multi-agent-mcp-server"
                : serverId,
        };
    }

    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += static (_, eventArgs) =>
        {
            try
            {
                Console.Error.WriteLine($"fatal.unhandled_exception={eventArgs.ExceptionObject}");
            }
            catch
            {
                // Avoid crash-looping while trying to report a crash.
            }
        };

        TaskScheduler.UnobservedTaskException += static (_, eventArgs) =>
        {
            try
            {
                Console.Error.WriteLine($"fatal.unobserved_task_exception={eventArgs.Exception}");
            }
            catch
            {
                // Avoid crash-looping while trying to report a crash.
            }

            eventArgs.SetObserved();
        };

        try
        {
            await CreateHost(args).RunAsync();
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            Console.Error.WriteLine($"fatal.host_termination={ex}");
            Environment.ExitCode = 1;
        }
    }
}
