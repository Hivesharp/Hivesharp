using Microsoft.Extensions.Logging;

namespace Hivesharp.Diagnostics;

internal static partial class WorkflowLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Workflow {WorkflowId} started (runId={RunId}, nodeCount={NodeCount})")]
    public static partial void Started(ILogger logger, string workflowId, string runId, int nodeCount);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information,
        Message = "Workflow {WorkflowId} completed (runId={RunId}, status={Status}, stepsExecuted={StepsExecuted}, durationMs={DurationMs})")]
    public static partial void Completed(ILogger logger, string workflowId, string runId, string status, int stepsExecuted, long durationMs);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information,
        Message = "Workflow {WorkflowId} suspended (runId={RunId}, suspendedAt={SuspendedAtStepId})")]
    public static partial void Suspended(ILogger logger, string workflowId, string runId, string suspendedAtStepId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information,
        Message = "Workflow {WorkflowId} resume started (runId={RunId}, fromStep={FromStep})")]
    public static partial void ResumeStarted(ILogger logger, string workflowId, string runId, string fromStep);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information,
        Message = "Workflow {WorkflowId} resume completed (runId={RunId}, status={Status}, durationMs={DurationMs})")]
    public static partial void ResumeCompleted(ILogger logger, string workflowId, string runId, string status, long durationMs);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Error,
        Message = "Workflow {WorkflowId} failed (runId={RunId})")]
    public static partial void Failed(ILogger logger, Exception ex, string workflowId, string runId);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Debug,
        Message = "Step {StepId} started")]
    public static partial void StepStarted(ILogger logger, string stepId);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Debug,
        Message = "Step {StepId} completed (status={Status}, durationMs={DurationMs})")]
    public static partial void StepCompleted(ILogger logger, string stepId, string status, long durationMs);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Error,
        Message = "Step {StepId} failed (durationMs={DurationMs})")]
    public static partial void StepFailed(ILogger logger, Exception ex, string stepId, long durationMs);

    [LoggerMessage(EventId = 2020, Level = LogLevel.Debug,
        Message = "Branch {BranchId} selected '{Branch}' (childCount={ChildCount})")]
    public static partial void BranchSelected(ILogger logger, string branchId, string branch, int childCount);

    [LoggerMessage(EventId = 2030, Level = LogLevel.Debug,
        Message = "Parallel {ParallelId} executed {ChildCount} step(s) in {DurationMs}ms")]
    public static partial void ParallelExecuted(ILogger logger, string parallelId, int childCount, long durationMs);
}
