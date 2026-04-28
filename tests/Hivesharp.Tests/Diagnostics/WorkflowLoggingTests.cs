using Hivesharp.Abstractions.Workflow;
using Hivesharp.Tests.Helpers;
using Hivesharp.Workflow;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Hivesharp.Tests.Diagnostics;

public class WorkflowLoggingTests
{
    [Fact]
    public async Task Sequential_Run_Emits_Started_Step_Started_Step_Completed_Completed()
    {
        var loggerFactory = new RecordingLoggerFactory();
        var services = new ServicesWithLogger(loggerFactory);

        var a = Step.Create("a", (object? i) => Task.FromResult<object?>("done"));
        var wf = new WorkflowBuilder("wf").Step(a).Build(services);

        var result = await wf.ExecuteAsync();

        Assert.Equal(WorkflowStatus.Completed, result.Status);
        Assert.Contains(loggerFactory.Records, r => r.EventId.Id == 2001);
        Assert.Contains(loggerFactory.Records, r => r.EventId.Id == 2010);
        Assert.Contains(loggerFactory.Records, r => r.EventId.Id == 2011);
        var completed = Assert.Single(loggerFactory.Records, r => r.EventId.Id == 2002);
        Assert.Contains("status=Completed", completed.Message);
    }

    [Fact]
    public async Task Throwing_Step_Emits_StepFailed_And_Failed()
    {
        var loggerFactory = new RecordingLoggerFactory();
        var services = new ServicesWithLogger(loggerFactory);

        var boom = Step.Create("boom",
            (object? _) => Task.FromException<object?>(new InvalidOperationException("boom")));
        var wf = new WorkflowBuilder("wf").Step(boom).Build(services);

        var result = await wf.ExecuteAsync();

        Assert.Equal(WorkflowStatus.Failed, result.Status);
        Assert.Contains(loggerFactory.Records, r => r.EventId.Id == 2012 && r.Level == LogLevel.Error);
        Assert.Contains(loggerFactory.Records, r => r.EventId.Id == 2006 && r.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Branch_Emits_BranchSelected()
    {
        var loggerFactory = new RecordingLoggerFactory();
        var services = new ServicesWithLogger(loggerFactory);

        var thenStep = Step.Create("then-step", (object? i) => Task.FromResult<object?>("then"));
        var elseStep = Step.Create("else-step", (object? i) => Task.FromResult<object?>("else"));

        var wf = new WorkflowBuilder("wf")
            .Branch(input => input is true,
                then: b => b.Step(thenStep),
                otherwise: b => b.Step(elseStep))
            .Build(services);

        await wf.ExecuteAsync(input: true);

        var branchRecord = Assert.Single(loggerFactory.Records, r => r.EventId.Id == 2020);
        Assert.Contains("then", branchRecord.Message);
    }

    private sealed class ServicesWithLogger(ILoggerFactory factory) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(ILoggerFactory) ? factory : null;
    }
}
