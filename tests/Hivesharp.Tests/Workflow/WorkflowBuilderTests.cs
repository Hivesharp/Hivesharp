using Hivesharp.Workflow;
using Xunit;

namespace Hivesharp.Tests.Workflow;

public class WorkflowBuilderTests
{
    [Fact]
    public void Build_Empty_Workflow_Throws()
    {
        var builder = new WorkflowBuilder("wf");
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Ctor_Null_Or_Empty_Id_Throws()
    {
        Assert.Throws<ArgumentException>(() => new WorkflowBuilder(""));
        Assert.Throws<ArgumentException>(() => new WorkflowBuilder("   "));
    }

    [Fact]
    public void Parallel_With_Single_Step_Throws()
    {
        var builder = new WorkflowBuilder("wf");
        var step = Step.Create("a", i => Task.FromResult<object?>(i));
        Assert.Throws<ArgumentException>(() => builder.Parallel(step));
    }

    [Fact]
    public void Step_Then_Chain_Builds_Descriptor_With_Sequential_Edges()
    {
        var a = Step.Create("a", i => Task.FromResult<object?>(i));
        var b = Step.Create("b", i => Task.FromResult<object?>(i));
        var c = Step.Create("c", i => Task.FromResult<object?>(i));

        var wf = new WorkflowBuilder("wf").Step(a).Then(b).Then(c).Build();

        Assert.Equal(["a", "b", "c"], wf.Descriptor.StepIds);
        Assert.Equal(3, wf.Descriptor.Nodes.Count);
        Assert.All(wf.Descriptor.Nodes, n => Assert.Equal("step", n.Type));
        Assert.Contains(wf.Descriptor.Edges, e => e.Source == "a" && e.Target == "b");
        Assert.Contains(wf.Descriptor.Edges, e => e.Source == "b" && e.Target == "c");
    }

    [Fact]
    public void Branch_Emits_Branch_And_Merge_Nodes_With_True_False_Labels()
    {
        var start = Step.Create("start", i => Task.FromResult<object?>(i));
        var hot = Step.Create("hot", i => Task.FromResult<object?>(i));
        var cold = Step.Create("cold", i => Task.FromResult<object?>(i));

        var wf = new WorkflowBuilder("wf")
            .Step(start)
            .Branch(i => true, b => b.Step(hot), b => b.Step(cold))
            .Build();

        Assert.Contains(wf.Descriptor.Nodes, n => n.Type == "branch");
        Assert.Contains(wf.Descriptor.Nodes, n => n.Type == "merge");
        Assert.Contains(wf.Descriptor.Edges, e => e.Target == "hot" && e.Label == "true");
        Assert.Contains(wf.Descriptor.Edges, e => e.Target == "cold" && e.Label == "false");
    }

    [Fact]
    public void Parallel_Emits_Fork_And_Join_Nodes()
    {
        var a = Step.Create("a", i => Task.FromResult<object?>(i));
        var b = Step.Create("b", i => Task.FromResult<object?>(i));

        var wf = new WorkflowBuilder("wf").Parallel(a, b).Build();

        Assert.Contains(wf.Descriptor.Nodes, n => n.Type == "parallel" && n.Label == "fork");
        Assert.Contains(wf.Descriptor.Nodes, n => n.Type == "parallel" && n.Label == "join");
        Assert.Contains(wf.Descriptor.StepIds, id => id == "a");
        Assert.Contains(wf.Descriptor.StepIds, id => id == "b");
    }
}
