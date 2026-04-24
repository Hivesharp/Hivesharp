using System.Diagnostics;
using Hivesharp.Abstractions.Workflow;

namespace Hivesharp.Workflow;

internal class Workflow(
    WorkflowDescriptor descriptor,
    IReadOnlyList<IWorkflowNode> nodes,
    IServiceProvider? serviceProvider = null,
    IWorkflowRunStore? runStore = null) : IWorkflow
{
    public WorkflowDescriptor Descriptor { get; } = descriptor;

    public async Task<WorkflowResult> ExecuteAsync(object? input = null, CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid().ToString();
        var context = new StepContext(serviceProvider ?? EmptyServiceProvider.Instance);
        var stepResults = new List<StepResult>();
        var current = input;

        try
        {
            foreach (var node in nodes)
            {
                var nodeResult = await node.ExecuteAsync(current, context, cancellationToken);
                stepResults.AddRange(nodeResult.Steps);

                if (nodeResult.IsSuspended)
                {
                    var snapshot = new WorkflowSnapshot
                    {
                        RunId = runId,
                        WorkflowId = Descriptor.Id,
                        CompletedSteps = stepResults.Where(s => s.Status == StepStatus.Completed).ToList(),
                        SuspendedAtStepId = nodeResult.SuspendedAtNodeId!,
                        SuspendPayload = nodeResult.SuspendPayload,
                        LastOutput = current
                    };

                    if (runStore is not null)
                        await runStore.SaveSnapshotAsync(snapshot, cancellationToken);

                    return new WorkflowResult
                    {
                        Status = WorkflowStatus.Suspended,
                        RunId = runId,
                        Output = current,
                        Steps = stepResults,
                        SuspendedStepId = nodeResult.SuspendedAtNodeId,
                        SuspendPayload = nodeResult.SuspendPayload
                    };
                }

                current = nodeResult.Output;
            }

            return new WorkflowResult
            {
                Status = WorkflowStatus.Completed,
                RunId = runId,
                Output = current,
                Steps = stepResults
            };
        }
        catch
        {
            return new WorkflowResult
            {
                Status = WorkflowStatus.Failed,
                RunId = runId,
                Output = current,
                Steps = stepResults
            };
        }
    }

    public async Task<WorkflowResult> ResumeAsync(string runId, object? resumeData = null, CancellationToken cancellationToken = default)
    {
        if (runStore is null)
            throw new InvalidOperationException("Cannot resume workflow: no IWorkflowRunStore configured.");

        var snapshot = await runStore.GetSnapshotAsync(runId, cancellationToken)
            ?? throw new InvalidOperationException($"No snapshot found for run '{runId}'.");

        var resumeContext = new StepContext(serviceProvider ?? EmptyServiceProvider.Instance, resumeData);
        var normalContext = new StepContext(serviceProvider ?? EmptyServiceProvider.Instance);
        var stepResults = new List<StepResult>(snapshot.CompletedSteps);
        var current = snapshot.LastOutput;
        var foundSuspended = false;

        try
        {
            foreach (var node in nodes)
            {
                if (!foundSuspended)
                {
                    if (node.Id == snapshot.SuspendedAtStepId)
                        foundSuspended = true;
                    else
                        continue;
                }

                var context = node.Id == snapshot.SuspendedAtStepId ? resumeContext : normalContext;
                var nodeResult = await node.ExecuteAsync(current, context, cancellationToken);
                stepResults.AddRange(nodeResult.Steps);

                if (nodeResult.IsSuspended)
                {
                    var newSnapshot = new WorkflowSnapshot
                    {
                        RunId = runId,
                        WorkflowId = Descriptor.Id,
                        CompletedSteps = stepResults.Where(s => s.Status == StepStatus.Completed).ToList(),
                        SuspendedAtStepId = nodeResult.SuspendedAtNodeId!,
                        SuspendPayload = nodeResult.SuspendPayload,
                        LastOutput = current
                    };

                    await runStore.SaveSnapshotAsync(newSnapshot, cancellationToken);

                    return new WorkflowResult
                    {
                        Status = WorkflowStatus.Suspended,
                        RunId = runId,
                        Output = current,
                        Steps = stepResults,
                        SuspendedStepId = nodeResult.SuspendedAtNodeId,
                        SuspendPayload = nodeResult.SuspendPayload
                    };
                }

                current = nodeResult.Output;
            }

            await runStore.DeleteSnapshotAsync(runId, cancellationToken);

            return new WorkflowResult
            {
                Status = WorkflowStatus.Completed,
                RunId = runId,
                Output = current,
                Steps = stepResults
            };
        }
        catch
        {
            return new WorkflowResult
            {
                Status = WorkflowStatus.Failed,
                RunId = runId,
                Output = current,
                Steps = stepResults
            };
        }
    }
}

internal record struct WorkflowNodeResult(
    object? Output,
    bool IsSuspended,
    object? SuspendPayload,
    string? SuspendedAtNodeId,
    IReadOnlyList<StepResult> Steps);

internal interface IWorkflowNode
{
    string Id { get; }
    Task<WorkflowNodeResult> ExecuteAsync(object? input, StepContext context, CancellationToken cancellationToken);
}

internal class StepNode(IStep step) : IWorkflowNode
{
    public string Id => step.Id;

    public async Task<WorkflowNodeResult> ExecuteAsync(object? input, StepContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await step.ExecuteAsync(input, context, cancellationToken);
            sw.Stop();

            if (result.IsSuspended)
            {
                return new WorkflowNodeResult(
                    null,
                    true,
                    result.SuspendPayload,
                    step.Id,
                    [new StepResult { StepId = step.Id, Status = StepStatus.Suspended, Duration = sw.Elapsed }]);
            }

            return new WorkflowNodeResult(
                result.Output,
                false,
                null,
                null,
                [new StepResult { StepId = step.Id, Status = StepStatus.Completed, Output = result.Output, Duration = sw.Elapsed }]);
        }
        catch
        {
            sw.Stop();
            throw;
        }
    }
}

internal class BranchNode(
    string id,
    Func<object?, bool> condition,
    IReadOnlyList<IWorkflowNode> thenNodes,
    IReadOnlyList<IWorkflowNode> otherwiseNodes) : IWorkflowNode
{
    public string Id => id;

    public async Task<WorkflowNodeResult> ExecuteAsync(object? input, StepContext context, CancellationToken cancellationToken)
    {
        var branch = condition(input) ? thenNodes : otherwiseNodes;
        var stepResults = new List<StepResult>();
        var current = input;

        foreach (var node in branch)
        {
            var nodeResult = await node.ExecuteAsync(current, context, cancellationToken);
            stepResults.AddRange(nodeResult.Steps);

            if (nodeResult.IsSuspended)
            {
                return new WorkflowNodeResult(
                    null,
                    true,
                    nodeResult.SuspendPayload,
                    nodeResult.SuspendedAtNodeId,
                    stepResults);
            }

            current = nodeResult.Output;
        }

        return new WorkflowNodeResult(current, false, null, null, stepResults);
    }
}

internal class ParallelNode(string id, IReadOnlyList<IStep> steps) : IWorkflowNode
{
    public string Id => id;

    public async Task<WorkflowNodeResult> ExecuteAsync(object? input, StepContext context, CancellationToken cancellationToken)
    {
        var tasks = steps.Select(async step =>
        {
            var sw = Stopwatch.StartNew();
            var result = await step.ExecuteAsync(input, context, cancellationToken);
            sw.Stop();

            if (result.IsSuspended)
                throw new InvalidOperationException($"Step '{step.Id}' called Suspend inside a parallel group, which is not supported.");

            return new StepResult
            {
                StepId = step.Id,
                Status = StepStatus.Completed,
                Output = result.Output,
                Duration = sw.Elapsed
            };
        }).ToList();

        var results = await Task.WhenAll(tasks);
        var output = results.ToDictionary(r => r.StepId, r => r.Output);

        return new WorkflowNodeResult(output, false, null, null, results.ToList());
    }
}

internal class EmptyServiceProvider : IServiceProvider
{
    public static readonly EmptyServiceProvider Instance = new();
    public object? GetService(Type serviceType) => null;
}
