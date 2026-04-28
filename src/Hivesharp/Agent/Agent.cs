using System.Diagnostics;
using Hivesharp.Abstractions.Agent;
using Hivesharp.Abstractions.Memory;
using Hivesharp.Diagnostics;
using Hivesharp.Memory;
using Hivesharp.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hivesharp.Agent;

internal class Agent(
    AgentDescriptor agentDescriptor,
    List<AITool>? tools,
    IChatClient chatClient,
    MemoryConfiguration? memory = null,
    IReadOnlyList<McpServerDefinition>? mcpServers = null,
    IMcpToolResolver? mcpToolResolver = null,
    ILogger<Agent>? logger = null) : IAgent
{
    private readonly ILogger _logger = logger ?? NullLogger<Agent>.Instance;

    public AgentDescriptor AgentDescriptor { get; } = agentDescriptor;
    public MemoryConfiguration? Memory { get; } = memory;

    private List<AITool>? _mcpTools;
    private volatile bool _mcpInitialized;
    private readonly SemaphoreSlim _mcpLock = new(1, 1);
    private long _lastMcpRetryAtTicks;
    private static readonly long McpRetryCooldownTicks = TimeSpan.FromSeconds(30).Ticks;

    private volatile AgentRuntimeState _runtimeState = mcpServers is { Count: > 0 }
        ? new AgentRuntimeState(
            mcpServers.Select(s => new McpServerStatus(s.Name, false, [], null)).ToList(),
            null)
        : AgentRuntimeState.Empty;

    public AgentRuntimeState RuntimeState => _runtimeState;

    private async Task EnsureMcpInitializedAsync(CancellationToken ct)
    {
        if (_mcpInitialized || mcpServers is not { Count: > 0 }) return;

        await _mcpLock.WaitAsync(ct);
        try
        {
            if (_mcpInitialized) return;
            var result = await mcpToolResolver!.ResolveToolsAsync(mcpServers, ct);
            _mcpTools = result.Tools;
            _runtimeState = new AgentRuntimeState(result.ServerStatuses, DateTimeOffset.UtcNow);
            _mcpInitialized = true;
        }
        finally
        {
            _mcpLock.Release();
        }
    }

    /// <summary>
    /// Returns agent instructions, appending a system note about unavailable MCP servers
    /// so the LLM knows not to attempt calling their tools.
    /// </summary>
    private string BuildInstructions()
    {
        var baseInstructions = AgentDescriptor.Instructions ?? string.Empty;

        if (!_mcpInitialized || mcpServers is not { Count: > 0 })
            return baseInstructions;

        var unavailable = _runtimeState.McpServers.Where(s => !s.IsAvailable).ToList();
        if (unavailable.Count == 0)
            return baseInstructions;

        var reasons = string.Join("; ", unavailable.Select(s =>
            $"{s.Name}: {s.UnavailableReason ?? "unavailable"}"));

        return baseInstructions +
            $"\n\n[SYSTEM: The following MCP servers failed to connect and their tools are NOT available in this session: {reasons}. " +
            "Do not attempt to call these tools. If the user asks about them, inform them that these services are currently unavailable.]";
    }

    private async Task AutoRetryFailedMcpAsync(CancellationToken cancellationToken)
    {
        if (!_mcpInitialized || !_runtimeState.McpServers.Any(s => !s.IsAvailable)) return;

        var now = DateTimeOffset.UtcNow.Ticks;
        var last = Interlocked.Read(ref _lastMcpRetryAtTicks);
        if (now - last <= McpRetryCooldownTicks) return;

        Interlocked.Exchange(ref _lastMcpRetryAtTicks, now);
        await RetryMcpAsync(cancellationToken);
    }

    public async Task RetryMcpAsync(CancellationToken cancellationToken = default)
    {
        if (mcpServers is not { Count: > 0 } || mcpToolResolver is null) return;

        if (!_mcpInitialized)
        {
            await EnsureMcpInitializedAsync(cancellationToken);
            return;
        }

        var failedNames = _runtimeState.McpServers
            .Where(s => !s.IsAvailable)
            .Select(s => s.Name)
            .ToHashSet();

        var toRetry = mcpServers.Where(s => failedNames.Contains(s.Name)).ToList();
        if (toRetry.Count == 0) return;

        McpLog.RetryAttempted(_logger, toRetry.Count);

        await _mcpLock.WaitAsync(cancellationToken);
        try
        {
            var result = await mcpToolResolver.ResolveToolsAsync(toRetry, cancellationToken);

            _mcpTools ??= [];
            _mcpTools.AddRange(result.Tools);

            var updated = _runtimeState.McpServers.ToList();
            foreach (var status in result.ServerStatuses)
            {
                var idx = updated.FindIndex(s => s.Name == status.Name);
                if (idx >= 0) updated[idx] = status;
                if (!status.IsAvailable)
                    McpLog.RetryFailed(_logger, status.Name);
            }
            _runtimeState = new AgentRuntimeState(updated, _runtimeState.LastInitializedAt);
        }
        finally
        {
            _mcpLock.Release();
        }
    }

    public async Task<AgentResult> GenerateAsync(string message, string? threadId, CancellationToken cancellationToken = default)
    {
        await EnsureMcpInitializedAsync(cancellationToken);
        await AutoRetryFailedMcpAsync(cancellationToken);

        if (Memory is null)
            return await GenerateSimpleAsync(message, cancellationToken);

        if (threadId is null)
        {
            var thread = await Memory.Storage.CreateThreadAsync(cancellationToken: cancellationToken);
            threadId = thread.Id;
        }

        AgentLog.GenerateStarted(_logger, AgentDescriptor.Id, threadId, message.Length);
        var sw = Stopwatch.StartNew();

        try
        {
            var chatOptions = new ChatOptions
            {
                Instructions = BuildInstructions()
            };

            await InjectWorkingMemoryAsync(threadId, chatOptions, cancellationToken);
            ApplyTools(chatOptions);

            var messages = await LoadMessageHistoryAsync(threadId, message, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return new AgentResult
                {
                    Completion = string.Empty,
                };
            }
            var response = await chatClient.GetResponseAsync(messages, chatOptions, cancellationToken);

            var completionText = await FlushWorkingMemoryAsync(threadId, response.Text ?? string.Empty);
            await PersistMessagesAsync(threadId, message, completionText);

            sw.Stop();
            AgentLog.GenerateCompleted(_logger, AgentDescriptor.Id, threadId,
                response.Usage?.InputTokenCount ?? 0,
                response.Usage?.OutputTokenCount ?? 0,
                sw.ElapsedMilliseconds);

            return new AgentResult
            {
                Completion = completionText,
                ThreadId = threadId,
                Usage = response.Usage.MapUsage(),
                ToolCalls = response.Messages.ExtractToolCalls()
            };
        }
        catch (Exception ex)
        {
            AgentLog.GenerateFailed(_logger, ex, AgentDescriptor.Id);
            throw;
        }
    }

    public async Task<AgentResult<T>> GenerateAsync<T>(string message, string? threadId = null, CancellationToken cancellationToken = default)
    {
        await EnsureMcpInitializedAsync(cancellationToken);
        await AutoRetryFailedMcpAsync(cancellationToken);

        if (Memory?.WorkingMemory is not null)
            throw new InvalidOperationException(
                "Structured output (GenerateAsync<T>) is incompatible with working memory; configure agent without WithWorkingMemory() or use the non-generic overload.");

        if (Memory is null)
            return await GenerateStructuredSimpleAsync<T>(message, cancellationToken);

        if (threadId is null)
        {
            var thread = await Memory.Storage.CreateThreadAsync(cancellationToken: cancellationToken);
            threadId = thread.Id;
        }

        AgentLog.GenerateStarted(_logger, AgentDescriptor.Id, threadId, message.Length);
        var sw = Stopwatch.StartNew();

        try
        {
            var chatOptions = new ChatOptions
            {
                Instructions = BuildInstructions()
            };

            ApplyTools(chatOptions);

            var messages = await LoadMessageHistoryAsync(threadId, message, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return new AgentResult<T> { Completion = string.Empty, IsValid = false };

            var response = await chatClient.GetResponseAsync<T>(messages, chatOptions, cancellationToken: cancellationToken);

            var rawJson = response.Text ?? string.Empty;
            var isValid = response.TryGetResult(out var typed);

            await PersistMessagesAsync(threadId, message, rawJson);

            sw.Stop();
            AgentLog.GenerateCompleted(_logger, AgentDescriptor.Id, threadId,
                response.Usage?.InputTokenCount ?? 0,
                response.Usage?.OutputTokenCount ?? 0,
                sw.ElapsedMilliseconds);

            return new AgentResult<T>
            {
                Completion = rawJson,
                ThreadId = threadId,
                Usage = response.Usage.MapUsage(),
                ToolCalls = response.Messages.ExtractToolCalls(),
                Result = isValid ? typed : default,
                IsValid = isValid
            };
        }
        catch (Exception ex)
        {
            AgentLog.GenerateFailed(_logger, ex, AgentDescriptor.Id);
            throw;
        }
    }

    private async Task<AgentResult<T>> GenerateStructuredSimpleAsync<T>(string message, CancellationToken cancellationToken)
    {
        AgentLog.SimpleGenerateStarted(_logger, AgentDescriptor.Id, message.Length);
        var sw = Stopwatch.StartNew();

        try
        {
            var options = new ChatOptions
            {
                Instructions = BuildInstructions()
            };

            ApplyTools(options);

            var response = await chatClient.GetResponseAsync<T>(message, options, cancellationToken: cancellationToken);

            sw.Stop();
            AgentLog.SimpleGenerateCompleted(_logger, AgentDescriptor.Id,
                response.Usage?.InputTokenCount ?? 0,
                response.Usage?.OutputTokenCount ?? 0,
                sw.ElapsedMilliseconds);

            var isValid = response.TryGetResult(out var typed);

            return new AgentResult<T>
            {
                Completion = response.Text ?? string.Empty,
                Usage = response.Usage.MapUsage(),
                ToolCalls = response.Messages.ExtractToolCalls(),
                Result = isValid ? typed : default,
                IsValid = isValid
            };
        }
        catch (Exception ex)
        {
            AgentLog.SimpleGenerateFailed(_logger, ex, AgentDescriptor.Id);
            throw;
        }
    }

    private async Task<AgentResult> GenerateSimpleAsync(string message, CancellationToken cancellationToken = default)
    {
        AgentLog.SimpleGenerateStarted(_logger, AgentDescriptor.Id, message.Length);
        var sw = Stopwatch.StartNew();

        try
        {
            var options = new ChatOptions
            {
                Instructions = BuildInstructions()
            };

            ApplyTools(options);

            var response = await chatClient.GetResponseAsync(message, options, cancellationToken);

            sw.Stop();
            AgentLog.SimpleGenerateCompleted(_logger, AgentDescriptor.Id,
                response.Usage?.InputTokenCount ?? 0,
                response.Usage?.OutputTokenCount ?? 0,
                sw.ElapsedMilliseconds);

            return new AgentResult
            {
                Completion = response.Text ?? string.Empty,
                Usage = response.Usage.MapUsage(),
                ToolCalls = response.Messages.ExtractToolCalls()
            };
        }
        catch (Exception ex)
        {
            AgentLog.SimpleGenerateFailed(_logger, ex, AgentDescriptor.Id);
            throw;
        }
    }

    private void ApplyTools(ChatOptions options)
    {
        var hasLocal = tools is { Count: > 0 };
        var hasMcp = _mcpTools is { Count: > 0 };
        if (!hasLocal && !hasMcp) return;

        var all = new List<AITool>();
        if (hasLocal) all.AddRange(tools!);
        if (hasMcp) all.AddRange(_mcpTools!);
        options.Tools = [..all];
    }

    private async Task InjectWorkingMemoryAsync(string threadId, ChatOptions chatOptions, CancellationToken cancellationToken)
    {
        if (Memory?.WorkingMemory is null) return;

        var storage = Memory.WorkingMemory.ResolveStorage(Memory.Storage);
        var workingMem = await storage.GetWorkingMemoryAsync(threadId, cancellationToken);
        chatOptions.Instructions = WorkingMemoryProcessor.BuildInstructions(chatOptions.Instructions ?? "", workingMem, Memory.WorkingMemory);
        AgentLog.WorkingMemoryInjected(_logger, threadId, !string.IsNullOrEmpty(workingMem));
    }

    private async Task<string> FlushWorkingMemoryAsync(string threadId, string completionText)
    {
        if (Memory?.WorkingMemory is null) return completionText;

        var (cleanedText, updatedMemory) = WorkingMemoryProcessor.ExtractWorkingMemory(completionText);
        if (updatedMemory is not null)
        {
            var storage = Memory.WorkingMemory.ResolveStorage(Memory.Storage);
            await storage.SaveWorkingMemoryAsync(threadId, updatedMemory);
        }
        AgentLog.WorkingMemoryFlushed(_logger, threadId, updatedMemory is not null);
        return cleanedText;
    }

    private async Task<List<ChatMessage>> LoadMessageHistoryAsync(string threadId, string message, CancellationToken cancellationToken)
    {
        var storage = Memory!.MessageHistory.ResolveStorage(Memory.Storage);
        var history = await storage.GetMessagesAsync(threadId, Memory.MessageHistory.MaxMessages, cancellationToken);
        var messages = history
            .Select(m => new ChatMessage(new ChatRole(m.Role), m.Content))
            .ToList();
        AgentLog.HistoryLoaded(_logger, messages.Count, threadId);
        messages.Add(new ChatMessage(ChatRole.User, message));
        return messages;
    }

    private async Task PersistMessagesAsync(string threadId, string userMessage, string assistantMessage)
    {
        var storage = Memory!.MessageHistory.ResolveStorage(Memory.Storage);
        await storage.SaveMessagesAsync(threadId,
        [
            new MemoryMessage { Role = "user", Content = userMessage, CreatedAt = DateTimeOffset.UtcNow },
            new MemoryMessage { Role = "assistant", Content = assistantMessage, CreatedAt = DateTimeOffset.UtcNow }
        ]);
        AgentLog.HistoryPersisted(_logger, 2, threadId);
    }
}
