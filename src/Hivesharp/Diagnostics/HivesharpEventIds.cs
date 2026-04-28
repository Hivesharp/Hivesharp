using Microsoft.Extensions.Logging;

namespace Hivesharp.Diagnostics;

/// <summary>
/// Stable EventId catalog for Hivesharp diagnostics.
/// Buckets: 1xxx = Agent, 2xxx = Workflow, 3xxx = MCP, 4xxx = RAG, 5xxx = Memory.
/// </summary>
internal static class HivesharpEventIds
{
    // Agent (1xxx)
    public static readonly EventId AgentGenerateStarted = new(1001, nameof(AgentGenerateStarted));
    public static readonly EventId AgentGenerateCompleted = new(1002, nameof(AgentGenerateCompleted));
    public static readonly EventId AgentGenerateFailed = new(1003, nameof(AgentGenerateFailed));
    public static readonly EventId AgentSimpleGenerateStarted = new(1011, nameof(AgentSimpleGenerateStarted));
    public static readonly EventId AgentSimpleGenerateCompleted = new(1012, nameof(AgentSimpleGenerateCompleted));
    public static readonly EventId AgentSimpleGenerateFailed = new(1013, nameof(AgentSimpleGenerateFailed));

    // Tool invocation (1100)
    public static readonly EventId ToolInvocationStarted = new(1101, nameof(ToolInvocationStarted));
    public static readonly EventId ToolInvocationCompleted = new(1102, nameof(ToolInvocationCompleted));
    public static readonly EventId ToolInvocationTimedOut = new(1103, nameof(ToolInvocationTimedOut));
    public static readonly EventId ToolInvocationFailed = new(1104, nameof(ToolInvocationFailed));

    // Workflow (2xxx)
    public static readonly EventId WorkflowStarted = new(2001, nameof(WorkflowStarted));
    public static readonly EventId WorkflowCompleted = new(2002, nameof(WorkflowCompleted));
    public static readonly EventId WorkflowSuspended = new(2003, nameof(WorkflowSuspended));
    public static readonly EventId WorkflowResumeStarted = new(2004, nameof(WorkflowResumeStarted));
    public static readonly EventId WorkflowResumeCompleted = new(2005, nameof(WorkflowResumeCompleted));
    public static readonly EventId WorkflowFailed = new(2006, nameof(WorkflowFailed));
    public static readonly EventId WorkflowStepStarted = new(2010, nameof(WorkflowStepStarted));
    public static readonly EventId WorkflowStepCompleted = new(2011, nameof(WorkflowStepCompleted));
    public static readonly EventId WorkflowStepFailed = new(2012, nameof(WorkflowStepFailed));
    public static readonly EventId WorkflowBranchSelected = new(2020, nameof(WorkflowBranchSelected));
    public static readonly EventId WorkflowParallelExecuted = new(2030, nameof(WorkflowParallelExecuted));

    // MCP (3xxx)
    public static readonly EventId McpServerConnectStarted = new(3001, nameof(McpServerConnectStarted));
    public static readonly EventId McpServerConnectCompleted = new(3002, nameof(McpServerConnectCompleted));
    public static readonly EventId McpServerConnectFailed = new(3003, nameof(McpServerConnectFailed));
    public static readonly EventId McpRetryAttempted = new(3010, nameof(McpRetryAttempted));
    public static readonly EventId McpRetryFailed = new(3011, nameof(McpRetryFailed));
    public static readonly EventId McpTransportCreated = new(3020, nameof(McpTransportCreated));
    public static readonly EventId McpPipeConnectTimedOut = new(3021, nameof(McpPipeConnectTimedOut));
    public static readonly EventId McpServerBootstrapped = new(3101, nameof(McpServerBootstrapped));
    public static readonly EventId McpAgentToolStarted = new(3110, nameof(McpAgentToolStarted));
    public static readonly EventId McpAgentToolCompleted = new(3111, nameof(McpAgentToolCompleted));
    public static readonly EventId McpAgentToolFailed = new(3112, nameof(McpAgentToolFailed));
    public static readonly EventId McpWorkflowToolStarted = new(3120, nameof(McpWorkflowToolStarted));
    public static readonly EventId McpWorkflowToolCompleted = new(3121, nameof(McpWorkflowToolCompleted));
    public static readonly EventId McpWorkflowToolFailed = new(3122, nameof(McpWorkflowToolFailed));

    // RAG (4xxx)
    public static readonly EventId RagIngestStarted = new(4001, nameof(RagIngestStarted));
    public static readonly EventId RagChunked = new(4002, nameof(RagChunked));
    public static readonly EventId RagEmbedded = new(4003, nameof(RagEmbedded));
    public static readonly EventId RagIngestCompleted = new(4004, nameof(RagIngestCompleted));
    public static readonly EventId RagIngestManyCompleted = new(4010, nameof(RagIngestManyCompleted));
    public static readonly EventId RagVectorQuery = new(4020, nameof(RagVectorQuery));

    // Memory (5xxx)
    public static readonly EventId MemoryHistoryLoaded = new(5001, nameof(MemoryHistoryLoaded));
    public static readonly EventId MemoryHistoryPersisted = new(5002, nameof(MemoryHistoryPersisted));
    public static readonly EventId WorkingMemoryInjected = new(5003, nameof(WorkingMemoryInjected));
    public static readonly EventId WorkingMemoryFlushed = new(5004, nameof(WorkingMemoryFlushed));
    public static readonly EventId WorkingMemoryParseFailed = new(5005, nameof(WorkingMemoryParseFailed));
}
