namespace Hivesharp.Storage.Redis;

internal sealed class RedisKeyBuilder(string prefix)
{
    private readonly string _prefix = string.IsNullOrWhiteSpace(prefix) ? "hivesharp" : prefix.TrimEnd(':');

    public string Thread(string threadId) => $"{_prefix}:memory:thread:{threadId}";
    public string ThreadMessages(string threadId) => $"{_prefix}:memory:thread:{threadId}:messages";
    public string ThreadWorkingMemory(string threadId) => $"{_prefix}:memory:thread:{threadId}:working";
    public string ResourceThreadsIndex(string resourceId) => $"{_prefix}:memory:resource:{resourceId}:threads";

    public string WorkflowRun(string runId) => $"{_prefix}:workflow:run:{runId}";
    public string WorkflowRunsIndex(string workflowId) => $"{_prefix}:workflow:{workflowId}:runs";
}
