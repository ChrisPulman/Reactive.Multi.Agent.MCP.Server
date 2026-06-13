using Microsoft.Extensions.DependencyInjection;
using Reactive.Multi.Agent.MCP.Core.Abstractions;
using Reactive.Multi.Agent.MCP.Server;
using System.Text;

namespace Reactive.Multi.Agent.MCP.Tests;

public class HostBootstrapTests
{
    [Test]
    public async Task CreateHost_Registers_Orchestration_Services()
    {
        using var host = Program.CreateHost([]);

        var agentCatalog = host.Services.GetRequiredService<IAgentCatalog>();
        var orchestration = host.Services.GetRequiredService<IOrchestrationService>();
        var options = host.Services.GetRequiredService<Reactive.Multi.Agent.MCP.Core.Configuration.ReactiveMultiAgentOptions>();

        await Assert.That(agentCatalog.GetAll().Count).IsGreaterThanOrEqualTo(15);
        await Assert.That(orchestration).IsNotNull();
        await Assert.That(options.StateRootPath).Contains("ReactiveMultiAgentMcp");
    }

    [Test]
    public async Task Program_Exception_Diagnostics_Write_Fatal_Messages_And_Observe_Unobserved_Task()
    {
        using var writer = new StringWriter();
        var unobservedArgs = new UnobservedTaskExceptionEventArgs(new AggregateException(new InvalidOperationException("Task failed.")));

        Program.WriteUnhandledExceptionDiagnostic(new UnhandledExceptionEventArgs(new InvalidOperationException("Unhandled failure."), isTerminating: true), writer);
        Program.WriteUnobservedTaskExceptionDiagnostic(unobservedArgs, writer);

        var output = writer.ToString();
        await Assert.That(output).Contains("fatal.unhandled_exception=");
        await Assert.That(output).Contains("Unhandled failure.");
        await Assert.That(output).Contains("fatal.unobserved_task_exception=");
        await Assert.That(output).Contains("Task failed.");
        await Assert.That(unobservedArgs.Observed).IsTrue();
    }

    [Test]
    public async Task Program_Exception_Diagnostics_Tolerate_Writer_Failures()
    {
        var unobservedArgs = new UnobservedTaskExceptionEventArgs(new AggregateException(new InvalidOperationException("Task failed.")));

        Program.WriteUnhandledExceptionDiagnostic(new UnhandledExceptionEventArgs(new InvalidOperationException("Unhandled failure."), isTerminating: true), new ThrowingTextWriter());
        Program.WriteUnobservedTaskExceptionDiagnostic(unobservedArgs, new ThrowingTextWriter());

        await Assert.That(unobservedArgs.Observed).IsTrue();
    }

    [Test]
    public async Task Program_Exception_Diagnostics_Default_Writers_Run_Without_Throwing()
    {
        var unobservedArgs = new UnobservedTaskExceptionEventArgs(new AggregateException(new InvalidOperationException("Default writer task failure.")));

        Program.WriteUnhandledExceptionDiagnostic(new UnhandledExceptionEventArgs(new InvalidOperationException("Default writer unhandled failure."), isTerminating: true));
        Program.WriteUnobservedTaskExceptionDiagnostic(unobservedArgs);

        await Assert.That(unobservedArgs.Observed).IsTrue();
    }

    [Test]
    public async Task Program_RunAsync_Writes_Host_Termination_And_Sets_ExitCode()
    {
        var originalExitCode = Environment.ExitCode;
        using var writer = new StringWriter();

        try
        {
            Environment.ExitCode = 0;

            await Program.RunAsync(() => throw new InvalidOperationException("Host failed."), writer);

            var output = writer.ToString();
            await Assert.That(output).Contains("fatal.host_termination=");
            await Assert.That(output).Contains("Host failed.");
            await Assert.That(Environment.ExitCode).IsEqualTo(1);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
        }
    }

    [Test]
    public async Task Program_RunAsync_Completes_When_Host_Delegate_Completes()
    {
        using var writer = new StringWriter();

        await Program.RunAsync(() => Task.CompletedTask, writer);

        await Assert.That(writer.ToString()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Program_Host_Termination_Diagnostic_Tolerates_Writer_Failure()
    {
        var completed = true;

        Program.WriteHostTerminationDiagnostic(new InvalidOperationException("Host failed."), new ThrowingTextWriter());

        await Assert.That(completed).IsTrue();
    }

    private sealed class ThrowingTextWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string? value)
            => throw new InvalidOperationException("Writer failed.");
    }
}
