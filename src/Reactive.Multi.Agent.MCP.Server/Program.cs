using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
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

/// <summary>
/// Provides the entry point and configuration methods for the Reactive Multi Agent MCP Server application.
/// </summary>
/// <remarks>This class is responsible for application startup, dependency injection configuration, and host
/// initialization. It sets up logging, service registrations, and server metadata, and handles unhandled exceptions at
/// the application level. The class is not intended to be instantiated.</remarks>
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

    public static Task Main(string[] args)
        => RunAsync(async () =>
        {
            using var host = CreateHost(args);
            await host.RunAsync();
        });

    internal static Task RunAsync(Func<Task> runHostAsync)
        => RunAsync(runHostAsync, Console.Error);

    internal static async Task RunAsync(Func<Task> runHostAsync, TextWriter errorWriter)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) => WriteUnhandledExceptionDiagnostic(eventArgs, errorWriter);
        TaskScheduler.UnobservedTaskException += (_, eventArgs) => WriteUnobservedTaskExceptionDiagnostic(eventArgs, errorWriter);

        try
        {
            await runHostAsync();
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            WriteHostTerminationDiagnostic(ex, errorWriter);
            Environment.ExitCode = 1;
        }
    }

    internal static void WriteUnhandledExceptionDiagnostic(UnhandledExceptionEventArgs eventArgs)
        => WriteUnhandledExceptionDiagnostic(eventArgs, Console.Error);

    internal static void WriteUnhandledExceptionDiagnostic(UnhandledExceptionEventArgs eventArgs, TextWriter errorWriter)
    {
        try
        {
            errorWriter.WriteLine($"fatal.unhandled_exception={eventArgs.ExceptionObject}");
        }
        catch
        {
            // Avoid crash-looping while trying to report a crash.
        }
    }

    internal static void WriteUnobservedTaskExceptionDiagnostic(UnobservedTaskExceptionEventArgs eventArgs)
        => WriteUnobservedTaskExceptionDiagnostic(eventArgs, Console.Error);

    internal static void WriteUnobservedTaskExceptionDiagnostic(UnobservedTaskExceptionEventArgs eventArgs, TextWriter errorWriter)
    {
        try
        {
            errorWriter.WriteLine($"fatal.unobserved_task_exception={eventArgs.Exception}");
        }
        catch
        {
            // Avoid crash-looping while trying to report a crash.
        }

        eventArgs.SetObserved();
    }

    internal static void WriteHostTerminationDiagnostic(Exception exception, TextWriter errorWriter)
    {
        try
        {
            errorWriter.WriteLine($"fatal.host_termination={exception}");
        }
        catch
        {
            // Avoid crash-looping while trying to report a crash.
        }
    }
}
