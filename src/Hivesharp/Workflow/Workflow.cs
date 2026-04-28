using System.Diagnostics;
using Hivesharp.Abstractions.Workflow;
using Hivesharp.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hivesharp.Workflow;

internal class Workflow(
    WorkflowDescriptor descriptor,
    IReadOnlyList<IWorkflowNode> nodes,
    IServiceProvider? serviceProvider = null,
    IWorkflowRunStore? runStore = null,
    ILogger<Workflow>? logger = null) : IWorkflow
{
    private readonly ILogger _logger = logger ?? NullLogger<Workflow>.Instance;

    public WorkflowDescriptor Descriptor { get; } = descriptor;

    public async Task<WorkflowResult> ExecuteAsync(object? input = null, CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid().ToString();
        var context = new StepContext(serviceProvider ?? EmptyServiceProvider.Instance);
        var stepResults = new List<StepResult>();
        var current = input;

        WorkflowLog.Started(_logger, Descriptor.Id, runId, nodes.Count);
        var sw = Stopwatch.StartNew();

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

                    sw.Stop();
                    WorkflowLog.Suspended(_logger, Descriptor.Id, runId, nodeResult.SuspendedAtNodeId!);
                    WorkflowLog.Completed(_logger, Descriptor.Id, runId, nameof(WorkflowStatus.Suspended), stepResults.Count, sw.ElapsedMilliseconds);

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

            sw.Stop();
            WorkflowLog.Completed(_logger, Descriptor.Id, runId, nameof(WorkflowStatus.Completed), stepResults.Count, sw.ElapsedMilliseconds);

            return new WorkflowResult
            {
                Status = WorkflowStatus.Completed,
                RunId = runId,
                Output = current,
                Steps = stepResults
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            WorkflowLog.Failed(_logger, ex, Descriptor.Id, runId);
            WorkflowLog.Completed(_logger, Descriptor.Id, runId, nameof(WorkflowStatus.Failed), stepResults.Count, sw.ElapsedMilliseconds);
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

        WorkflowLog.ResumeStarted(_logger, Descriptor.Id, runId, snapshot.SuspendedAtStepId);
        var sw = Stopwatch.StartNew();

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

                    sw.Stop();
                    WorkflowLog.Suspended(_logger, Descriptor.Id, runId, nodeResult.SuspendedAtNodeId!);
                    WorkflowLog.ResumeCompleted(_logger, Descriptor.Id, runId, nameof(WorkflowStatus.Suspended), sw.ElapsedMilliseconds);

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

            sw.Stop();
            WorkflowLog.ResumeCompleted(_logger, Descriptor.Id, runId, nameof(WorkflowStatus.Completed), sw.ElapsedMilliseconds);

            return new WorkflowResult
            {
                Status = WorkflowStatus.Completed,
                RunId = runId,
                Output = current,
                Steps = stepResults
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            WorkflowLog.Failed(_logger, ex, Descriptor.Id, runId);
            WorkflowLog.ResumeCompleted(_logger, Descriptor.Id, runId, nameof(WorkflowStatus.Failed), sw.ElapsedMilliseconds);
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

internal static class NodeLoggerHelper
{
    public static ILogger GetLogger(StepContext context)
    {
        var factory = context.Services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        return factory?.CreateLogger("Hivesharp.Workflow") ?? NullLogger.Instance;
    }
}

internal class StepNode(IStep step) : IWorkflowNode
{
    public string Id => step.Id;

    public async Task<WorkflowNodeResult> ExecuteAsync(object? input, StepContext context, CancellationToken cancellationToken)
    {
        var logger = NodeLoggerHelper.GetLogger(context);
        WorkflowLog.StepStarted(logger, step.Id);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await step.ExecuteAsync(input, context, cancellationToken);
            sw.Stop();

            if (result.IsSuspended)
            {
                WorkflowLog.StepCompleted(logger, step.Id, nameof(StepStatus.Suspended), sw.ElapsedMilliseconds);
                return new WorkflowNodeResult(
                    null,
                    true,
                    result.SuspendPayload,
                    step.Id,
                    [new StepResult { StepId = step.Id, Status = StepStatus.Suspended, Duration = sw.Elapsed }]);
            }

            WorkflowLog.StepCompleted(logger, step.Id, nameof(StepStatus.Completed), sw.ElapsedMilliseconds);
            return new WorkflowNodeResult(
                result.Output,
                false,
                null,
                null,
                [new StepResult { StepId = step.Id, Status = StepStatus.Completed, Output = result.Output, Duration = sw.Elapsed }]);
        }
        catch (Exception ex)
        {
            sw.Stop();
            WorkflowLog.StepFailed(logger, ex, step.Id, sw.ElapsedMilliseconds);
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
        var logger = NodeLoggerHelper.GetLogger(context);
        var taken = condition(input);
        var branch = taken ? thenNodes : otherwiseNodes;
        WorkflowLog.BranchSelected(logger, id, taken ? "then" : "otherwise", branch.Count);

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
        var logger = NodeLoggerHelper.GetLogger(context);
        var sw = Stopwatch.StartNew();
        var tasks = steps.Select(async step =>
        {
            var stepSw = Stopwatch.StartNew();
            var result = await step.ExecuteAsync(input, context, cancellationToken);
            stepSw.Stop();

            if (result.IsSuspended)
                throw new InvalidOperationException($"Step '{step.Id}' called Suspend inside a parallel group, which is not supported.");

            return new StepResult
            {
                StepId = step.Id,
                Status = StepStatus.Completed,
                Output = result.Output,
                Duration = stepSw.Elapsed
            };
        }).ToList();

        var results = await Task.WhenAll(tasks);
        sw.Stop();
        WorkflowLog.ParallelExecuted(logger, id, steps.Count, sw.ElapsedMilliseconds);
        var output = results.ToDictionary(r => r.StepId, r => r.Output);

        return new WorkflowNodeResult(output, false, null, null, results.ToList());
    }
}

internal class EmptyServiceProvider : IServiceProvider
{
    public static readonly EmptyServiceProvider Instance = new();
    public object? GetService(Type serviceType) => null;
}
