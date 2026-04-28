using Hivesharp.Abstractions.Workflow;
using Microsoft.Extensions.Logging;

namespace Hivesharp.Workflow;

public class WorkflowBuilder
{
    private readonly string _id;
    private readonly List<IWorkflowNode> _nodes = [];
    private readonly List<string> _stepIds = [];
    private readonly List<GraphEntry> _graphEntries = [];
    private int _branchCounter;
    private int _parallelCounter;

    public WorkflowBuilder(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        _id = id;
    }

    public WorkflowBuilder Step(IStep step)
    {
        _nodes.Add(new StepNode(step));
        _stepIds.Add(step.Id);
        _graphEntries.Add(new StepEntry(step.Id));
        return this;
    }

    public WorkflowBuilder Then(IStep step) => Step(step);

    public WorkflowBuilder Branch(
        Func<object?, bool> when,
        Action<WorkflowBuilder> then,
        Action<WorkflowBuilder>? otherwise = null)
    {
        var thenBuilder = new WorkflowBuilder($"{_id}/then");
        then(thenBuilder);

        var otherwiseBuilder = new WorkflowBuilder($"{_id}/otherwise");
        otherwise?.Invoke(otherwiseBuilder);

        var branchId = $"branch-{_branchCounter++}";
        _nodes.Add(new BranchNode(branchId, when, thenBuilder._nodes, otherwiseBuilder._nodes));
        _stepIds.AddRange(thenBuilder._stepIds);
        _stepIds.AddRange(otherwiseBuilder._stepIds);
        _graphEntries.Add(new BranchEntry(thenBuilder._graphEntries, otherwiseBuilder._graphEntries));

        return this;
    }

    public WorkflowBuilder Parallel(params IStep[] steps)
    {
        if (steps.Length < 2)
            throw new ArgumentException("Parallel requires at least 2 steps.", nameof(steps));

        var parallelId = $"parallel-{_parallelCounter++}";
        _nodes.Add(new ParallelNode(parallelId, steps));
        _stepIds.AddRange(steps.Select(s => s.Id));
        _graphEntries.Add(new ParallelEntry(steps.Select(s => s.Id).ToList()));
        return this;
    }

    public IWorkflow Build(IServiceProvider? serviceProvider = null)
    {
        if (_nodes.Count == 0)
            throw new InvalidOperationException("Workflow must have at least one step.");

        var (nodes, edges) = BuildGraph();

        var descriptor = new WorkflowDescriptor
        {
            Id = _id,
            StepIds = _stepIds,
            Nodes = nodes,
            Edges = edges,
        };

        var runStore = serviceProvider?.GetService(typeof(IWorkflowRunStore)) as IWorkflowRunStore;
        var loggerFactory = serviceProvider?.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        var logger = loggerFactory?.CreateLogger<Workflow>();

        return new Workflow(descriptor, _nodes, serviceProvider, runStore, logger);
    }

    private (List<WorkflowNodeDescriptor>, List<WorkflowEdgeDescriptor>) BuildGraph()
    {
        var nodes = new List<WorkflowNodeDescriptor>();
        var edges = new List<WorkflowEdgeDescriptor>();
        var branchCounter = 0;
        var parallelCounter = 0;

        string? previousId = null;

        foreach (var entry in _graphEntries)
        {
            if (entry is StepEntry step)
            {
                nodes.Add(new WorkflowNodeDescriptor { Id = step.StepId, Type = "step", Label = step.StepId });
                if (previousId is not null)
                    edges.Add(new WorkflowEdgeDescriptor { Source = previousId, Target = step.StepId });
                previousId = step.StepId;
            }
            else if (entry is BranchEntry branch)
            {
                var branchId = $"branch-{branchCounter++}";
                nodes.Add(new WorkflowNodeDescriptor { Id = branchId, Type = "branch", Label = "condition" });

                if (previousId is not null)
                    edges.Add(new WorkflowEdgeDescriptor { Source = previousId, Target = branchId });

                // Merge node after branch
                var mergeId = $"{branchId}-merge";
                nodes.Add(new WorkflowNodeDescriptor { Id = mergeId, Type = "merge" });

                // Then branch
                var thenPrev = branchId;
                foreach (var thenEntry in branch.ThenEntries)
                {
                    if (thenEntry is StepEntry ts)
                    {
                        nodes.Add(new WorkflowNodeDescriptor { Id = ts.StepId, Type = "step", Label = ts.StepId });
                        edges.Add(new WorkflowEdgeDescriptor
                        {
                            Source = thenPrev, Target = ts.StepId,
                            Label = thenPrev == branchId ? "true" : null
                        });
                        thenPrev = ts.StepId;
                    }
                }
                edges.Add(new WorkflowEdgeDescriptor { Source = thenPrev, Target = mergeId });

                // Otherwise branch
                var elsePrev = branchId;
                foreach (var elseEntry in branch.OtherwiseEntries)
                {
                    if (elseEntry is StepEntry es)
                    {
                        nodes.Add(new WorkflowNodeDescriptor { Id = es.StepId, Type = "step", Label = es.StepId });
                        edges.Add(new WorkflowEdgeDescriptor
                        {
                            Source = elsePrev, Target = es.StepId,
                            Label = elsePrev == branchId ? "false" : null
                        });
                        elsePrev = es.StepId;
                    }
                }
                edges.Add(new WorkflowEdgeDescriptor { Source = elsePrev, Target = mergeId });

                previousId = mergeId;
            }
            else if (entry is ParallelEntry parallel)
            {
                var forkId = $"parallel-fork-{parallelCounter}";
                var joinId = $"parallel-join-{parallelCounter}";
                parallelCounter++;

                nodes.Add(new WorkflowNodeDescriptor { Id = forkId, Type = "parallel", Label = "fork" });
                nodes.Add(new WorkflowNodeDescriptor { Id = joinId, Type = "parallel", Label = "join" });

                if (previousId is not null)
                    edges.Add(new WorkflowEdgeDescriptor { Source = previousId, Target = forkId });

                foreach (var stepId in parallel.StepIds)
                {
                    nodes.Add(new WorkflowNodeDescriptor { Id = stepId, Type = "step", Label = stepId });
                    edges.Add(new WorkflowEdgeDescriptor { Source = forkId, Target = stepId });
                    edges.Add(new WorkflowEdgeDescriptor { Source = stepId, Target = joinId });
                }

                previousId = joinId;
            }
        }

        return (nodes, edges);
    }

    private abstract record GraphEntry;
    private record StepEntry(string StepId) : GraphEntry;
    private record BranchEntry(List<GraphEntry> ThenEntries, List<GraphEntry> OtherwiseEntries) : GraphEntry;
    private record ParallelEntry(List<string> StepIds) : GraphEntry;
}
