using Hivesharp.Abstractions.Workflow;
using Hivesharp.Workflow;
using Moq;
using Xunit;

namespace Hivesharp.Tests.Workflow;

public class WorkflowExecutionTests
{
    [Fact]
    public async Task Sequential_Steps_Complete_With_Output_And_StepResults()
    {
        var a = Step.Create("a", (object? i) => Task.FromResult<object?>((int)(i ?? 0) + 1));
        var b = Step.Create("b", (object? i) => Task.FromResult<object?>((int)(i ?? 0) * 10));

        var wf = new WorkflowBuilder("wf").Step(a).Then(b).Build();

        var result = await wf.ExecuteAsync(input: 1);

        Assert.Equal(WorkflowStatus.Completed, result.Status);
        Assert.Equal(20, result.Output);
        Assert.Equal(2, result.Steps.Count);
        Assert.All(result.Steps, s => Assert.Equal(StepStatus.Completed, s.Status));
    }

    [Fact]
    public async Task Step_Throwing_Returns_Failed()
    {
        var boom = Step.Create("boom",
            (object? _) => Task.FromException<object?>(new InvalidOperationException("boom")));

        var wf = new WorkflowBuilder("wf").Step(boom).Build();
        var result = await wf.ExecuteAsync();

        Assert.Equal(WorkflowStatus.Failed, result.Status);
    }

    [Fact]
    public async Task Suspend_Persists_Snapshot_And_Returns_Suspended_Result()
    {
        var store = new Mock<IWorkflowRunStore>();
        WorkflowSnapshot? saved = null;
        store.Setup(s => s.SaveSnapshotAsync(It.IsAny<WorkflowSnapshot>(), It.IsAny<CancellationToken>()))
             .Callback<WorkflowSnapshot, CancellationToken>((snap, _) => saved = snap)
             .Returns(Task.CompletedTask);

        var services = new SimpleServiceProvider();
        services.Register(typeof(IWorkflowRunStore), store.Object);

        var hitl = Step.Create("review", (object? input, StepContext ctx, CancellationToken _) =>
            Task.FromResult(ctx.IsResuming
                ? StepExecutionResult.Continue(ctx.ResumeData)
                : StepExecutionResult.Suspend(new { draft = input })));

        var wf = new WorkflowBuilder("wf").Step(hitl).Build(services);

        var result = await wf.ExecuteAsync(input: "hello");

        Assert.Equal(WorkflowStatus.Suspended, result.Status);
        Assert.Equal("review", result.SuspendedStepId);
        Assert.NotNull(result.SuspendPayload);
        Assert.NotNull(result.RunId);
        Assert.NotNull(saved);
        Assert.Equal("review", saved!.SuspendedAtStepId);
    }

    [Fact]
    public async Task Resume_Without_RunStore_Throws()
    {
        var step = Step.Create("a", (object? i) => Task.FromResult<object?>(i));
        var wf = new WorkflowBuilder("wf").Step(step).Build();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await wf.ResumeAsync("missing"));
    }

    [Fact]
    public async Task Resume_With_Unknown_RunId_Throws()
    {
        var store = new Mock<IWorkflowRunStore>();
        store.Setup(s => s.GetSnapshotAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((WorkflowSnapshot?)null);

        var services = new SimpleServiceProvider();
        services.Register(typeof(IWorkflowRunStore), store.Object);

        var step = Step.Create("a", (object? i) => Task.FromResult<object?>(i));
        var wf = new WorkflowBuilder("wf").Step(step).Build(services);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await wf.ResumeAsync("unknown-id"));
    }

    [Fact]
    public async Task Resume_Continues_From_Suspended_Step_With_ResumeData()
    {
        var store = new Mock<IWorkflowRunStore>();
        WorkflowSnapshot? persisted = null;

        store.Setup(s => s.SaveSnapshotAsync(It.IsAny<WorkflowSnapshot>(), It.IsAny<CancellationToken>()))
             .Callback<WorkflowSnapshot, CancellationToken>((snap, _) => persisted = snap)
             .Returns(Task.CompletedTask);

        store.Setup(s => s.GetSnapshotAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(() => persisted);

        store.Setup(s => s.DeleteSnapshotAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var services = new SimpleServiceProvider();
        services.Register(typeof(IWorkflowRunStore), store.Object);

        var hitl = Step.Create("review", (object? input, StepContext ctx, CancellationToken _) =>
            Task.FromResult(ctx.IsResuming
                ? StepExecutionResult.Continue(ctx.ResumeData)
                : StepExecutionResult.Suspend(new { draft = input })));

        var finalize = Step.Create("finalize",
            (object? i) => Task.FromResult<object?>($"done:{i}"));

        var wf = new WorkflowBuilder("wf").Step(hitl).Then(finalize).Build(services);

        var first = await wf.ExecuteAsync(input: "hi");
        Assert.Equal(WorkflowStatus.Suspended, first.Status);

        var resumed = await wf.ResumeAsync(first.RunId!, resumeData: "approved");

        Assert.Equal(WorkflowStatus.Completed, resumed.Status);
        Assert.Equal("done:approved", resumed.Output);
        store.Verify(s => s.DeleteSnapshotAsync(first.RunId!, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Branch_True_Executes_Then_Branch_Only()
    {
        var hot = Step.Create("hot", (object? _) => Task.FromResult<object?>("HOT"));
        var cold = Step.Create("cold", (object? _) => Task.FromResult<object?>("COLD"));

        var wf = new WorkflowBuilder("wf")
            .Branch(i => true, b => b.Step(hot), b => b.Step(cold))
            .Build();

        var result = await wf.ExecuteAsync();

        Assert.Equal(WorkflowStatus.Completed, result.Status);
        Assert.Equal("HOT", result.Output);
        Assert.Contains(result.Steps, s => s.StepId == "hot");
        Assert.DoesNotContain(result.Steps, s => s.StepId == "cold");
    }

    [Fact]
    public async Task Branch_False_Executes_Otherwise_Branch()
    {
        var hot = Step.Create("hot", (object? _) => Task.FromResult<object?>("HOT"));
        var cold = Step.Create("cold", (object? _) => Task.FromResult<object?>("COLD"));

        var wf = new WorkflowBuilder("wf")
            .Branch(i => false, b => b.Step(hot), b => b.Step(cold))
            .Build();

        var result = await wf.ExecuteAsync();

        Assert.Equal("COLD", result.Output);
    }

    [Fact]
    public async Task Parallel_Runs_All_Steps_And_Aggregates_Output()
    {
        var a = Step.Create("a", (object? _) => Task.FromResult<object?>(1));
        var b = Step.Create("b", (object? _) => Task.FromResult<object?>(2));

        var wf = new WorkflowBuilder("wf").Parallel(a, b).Build();

        var result = await wf.ExecuteAsync();

        Assert.Equal(WorkflowStatus.Completed, result.Status);
        var dict = Assert.IsAssignableFrom<IDictionary<string, object?>>(result.Output);
        Assert.Equal(1, dict["a"]);
        Assert.Equal(2, dict["b"]);
    }

    [Fact]
    public async Task Parallel_With_Suspend_Returns_Failed()
    {
        var a = Step.Create("a", (object? _) => Task.FromResult<object?>(1));
        var bad = Step.Create("bad",
            (object? _, CancellationToken __) => Task.FromResult(StepExecutionResult.Suspend("nope")));

        var wf = new WorkflowBuilder("wf").Parallel(a, bad).Build();

        var result = await wf.ExecuteAsync();

        Assert.Equal(WorkflowStatus.Failed, result.Status);
    }

    [Fact]
    public async Task Parallel_With_Suspend_Throws_WorkflowConfigurationException_Naming_Step_And_Group()
    {
        var a = Step.Create("a", (object? _) => Task.FromResult<object?>(1));
        var bad = Step.Create("bad",
            (object? _, CancellationToken __) => Task.FromResult(StepExecutionResult.Suspend("nope")));

        var node = new ParallelNode("parallel-0", new IStep[] { a, bad });
        var ctx = new StepContext(new SimpleServiceProvider());

        var ex = await Assert.ThrowsAsync<WorkflowConfigurationException>(async () =>
            await node.ExecuteAsync(input: null, ctx, CancellationToken.None));

        Assert.Contains("'bad'", ex.Message);
        Assert.Contains("'parallel-0'", ex.Message);
        Assert.Contains("https://hivesharp.dev/concepts/workflows#suspend-in-parallel", ex.Message);
    }

    private sealed class SimpleServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];
        public void Register(Type t, object instance) => _services[t] = instance;
        public object? GetService(Type serviceType) => _services.GetValueOrDefault(serviceType);
    }
}
