using Hivesharp.Abstractions.Agent;
using Hivesharp.Abstractions.AgentBuilder;
using Hivesharp.Abstractions.Memory;
using Hivesharp.Abstractions.Mcp;
using Hivesharp.Abstractions.Rag;
using Hivesharp.Abstractions.Tool;
using Hivesharp.Agent.Contracts;
using Hivesharp.Mcp;
using Hivesharp.Rag;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hivesharp.Agent.Builder;

internal sealed class AgentBuilder(
    IAgentBuilderChatClientFactory agentBuilderChatClientFactory,
    IMemoryStorage? defaultStorage = null,
    IVectorStore? defaultVectorStore = null,
    ITextEmbedder? defaultTextEmbedder = null,
    IMcpToolResolver? mcpToolResolver = null,
    IServiceProvider? serviceProvider = null,
    ILoggerFactory? loggerFactory = null) : IAgentBuilder
{
    private string? _id;
    private string? _modelName;
    private string? _providerName;
    private string? _instructions;
    private IDictionary<string, ITool>? _tools;
    private int _maxSteps = 10;
    private MessageHistoryConfiguration? _messageHistory;
    private WorkingMemoryConfiguration? _workingMemory;
    private VectorQueryToolConfig? _vectorQueryToolConfig;
    private List<McpServerDefinition>? _mcpServers;

    public IAgentBuilderSetup WithId(string id)
    {
        _id = id;
        return this;
    }
    public IAgentBuilderSetup WithModel(string providerName, string modelName)
    {
        _providerName = providerName;
        _modelName = modelName;
        return this;
    }
    public IAgentBuilderSetup WithModel(string providerNameColonModelName)
    {
        var values = providerNameColonModelName.Split(':');

        if(values.Length != 2)
        {
            throw new AgentBuilderModelNotProvidedException();
        }

        WithModel(values[0], values[1]);

        return this;
    }
    public IAgentBuilderSetup WithInstructions(string instructions)
    {
        _instructions = instructions;
        return this;
    }
    public IAgentBuilderSetup WithTool(Type type)
    {
        if (!typeof(ITool).IsAssignableFrom(type))
            throw new AgentBuilderIncorrectToolTypeException();

        var instance = serviceProvider is not null
            ? (ITool)ActivatorUtilities.CreateInstance(serviceProvider, type)
            : (ITool)(Activator.CreateInstance(type)
                ?? throw new AgentBuilderIncorrectToolTypeException());

        return WithTool(instance);
    }
    public IAgentBuilderSetup WithTool<T>() where T : ITool => WithTool(typeof(T));
    public IAgentBuilderSetup WithTool(ITool instance)
    {
        _tools ??= new Dictionary<string, ITool>();
        _tools.TryAdd(instance.Name, instance);
        return this;
    }
    public IAgentBuilderSetup WithTool(string name, string description, Delegate handler)
        => WithTool(new DelegateTool(name, description, handler));
    public IAgentBuilderSetup WithMaxSteps(int maxSteps)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSteps, 1);
        _maxSteps = maxSteps;
        return this;
    }
    public IAgentBuilderSetup WithMessageHistoryMemory(int? maxMessages = null)
    {
        _messageHistory = new MessageHistoryConfiguration { MaxMessages = maxMessages ?? 40 };
        return this;
    }
    public IAgentBuilderSetup WithMessageHistoryMemory(IMemoryStorage storage, int? maxMessages = null)
    {
        _messageHistory = new MessageHistoryConfiguration { Storage = storage, MaxMessages = maxMessages ?? 40 };
        return this;
    }
    public IAgentBuilderSetup WithMessageHistoryMemory(Type storageType, int? maxMessages = null)
        => WithMessageHistoryMemory(ResolveStorage(storageType), maxMessages);
    public IAgentBuilderSetup WithMessageHistoryMemory<TStorage>(int? maxMessages = null)
        where TStorage : class, IMemoryStorage
        => WithMessageHistoryMemory(typeof(TStorage), maxMessages);
    public IAgentBuilderSetup WithWorkingMemory(string? instructions = null)
    {
        _workingMemory = new WorkingMemoryConfiguration { Instructions = instructions };
        return this;
    }
    public IAgentBuilderSetup WithWorkingMemory(IMemoryStorage storage, string? instructions = null)
    {
        _workingMemory = new WorkingMemoryConfiguration { Storage = storage, Instructions = instructions };
        return this;
    }
    public IAgentBuilderSetup WithWorkingMemory(Type storageType, string? instructions = null)
        => WithWorkingMemory(ResolveStorage(storageType), instructions);
    public IAgentBuilderSetup WithWorkingMemory<TStorage>(string? instructions = null)
        where TStorage : class, IMemoryStorage
        => WithWorkingMemory(typeof(TStorage), instructions);

    private IMemoryStorage ResolveStorage(Type storageType)
    {
        if (!typeof(IMemoryStorage).IsAssignableFrom(storageType))
            throw new ArgumentException($"Type '{storageType.FullName}' does not implement {nameof(IMemoryStorage)}.", nameof(storageType));

        return serviceProvider is not null
            ? (IMemoryStorage)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, storageType)
            : (IMemoryStorage)(Activator.CreateInstance(storageType)
                ?? throw new InvalidOperationException($"Could not activate '{storageType.FullName}'."));
    }
    public IAgentBuilderSetup WithVectorQueryTool(string indexName, string? toolDescription = null, int topK = 5, IReadOnlyDictionary<string, object?>? filter = null)
    {
        if (filter is not null)
        {
            foreach (var key in filter.Keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    throw new ArgumentException("Metadata filter keys must be non-empty.", nameof(filter));
            }
        }

        _vectorQueryToolConfig = new VectorQueryToolConfig(indexName, toolDescription, topK, filter);
        return this;
    }
    public IAgentBuilderSetup WithMcpServer(string name, Uri httpEndpoint)
    {
        _mcpServers ??= [];
        _mcpServers.Add(new McpServerDefinition(name, httpEndpoint, null));
        return this;
    }
    public IAgentBuilderSetup WithMcpServer(string name, string pipeName)
    {
        _mcpServers ??= [];
        _mcpServers.Add(new McpServerDefinition(name, null, pipeName));
        return this;
    }

    public IAgent Build()
    {
        if (_providerName == null || _modelName == null)
            throw new AgentBuilderModelNotProvidedException();

        if (_mcpServers is { Count: > 0 } && mcpToolResolver is null)
            throw new InvalidOperationException(
                "MCP servers are configured but no IMcpToolResolver is available. Register MCP support with AddHivesharpMcp().");

        BuildVectorQueryTool();

        var (aiTools, chatClient) = CreateTools(
            agentBuilderChatClientFactory.GetChatClient(_providerName, _modelName));

        var descriptor = CreateDescriptor();
        var memory = BuildMemoryConfiguration();

        var agentLogger = loggerFactory?.CreateLogger<global::Hivesharp.Agent.Agent>();
        return new global::Hivesharp.Agent.Agent(descriptor, aiTools, chatClient, memory, _mcpServers, mcpToolResolver, agentLogger);
    }

    private (List<AITool>?, IChatClient) CreateTools(IChatClient chatClient)
    {
        var hasLocalTools = _tools is { Count: > 0 };
        var hasMcpServers = _mcpServers is { Count: > 0 };

        if (!hasLocalTools && !hasMcpServers)
            return (null, chatClient);

        var aiTools = new List<AITool>();

        if (hasLocalTools)
        {
            aiTools.AddRange(_tools!.Values
                .Select(t => AIFunctionFactory.Create(t.GetDelegate(), t.Name, t.Description))
                .Cast<AITool>());
        }

        var invokingClient = new TimeoutFunctionInvokingChatClient(
            chatClient,
            _maxSteps,
            TimeSpan.FromSeconds(10),
            loggerFactory?.CreateLogger<TimeoutFunctionInvokingChatClient>());

        return (aiTools.Count > 0 ? aiTools : null, invokingClient);
    }

    private AgentDescriptor CreateDescriptor()
    {
        var toolNames = _tools?.Values.Select(t => t.Name).ToList() ?? [];

        var mcpServerDescriptors = _mcpServers?.Select(s => new McpServerDescriptor
        {
            Name = s.Name,
            TransportType = s.PipeName is not null ? "pipe" : "http"
        }).ToList<McpServerDescriptor>() ?? [];

        return new AgentDescriptor
        {
            Id = _id ?? Guid.NewGuid().ToString(),
            Model = _modelName!,
            Instructions = _instructions,
            ToolNames = toolNames,
            HasMemory = _messageHistory is not null || _workingMemory is not null,
            McpServers = mcpServerDescriptors
        };
    }

    private MemoryConfiguration? BuildMemoryConfiguration()
    {
        if (_messageHistory is null && _workingMemory is null)
            return null;

        var storage = _messageHistory?.Storage ?? _workingMemory?.Storage ?? defaultStorage
            ?? throw new InvalidOperationException(
                "No IMemoryStorage available. Register a default with AddMemoryStorage<T>() or pass one explicitly.");

        return new MemoryConfiguration
        {
            Storage = storage,
            MessageHistory = _messageHistory ?? new MessageHistoryConfiguration(),
            WorkingMemory = _workingMemory
        };
    }

    private void BuildVectorQueryTool()
    {
        if (_vectorQueryToolConfig is null)
            return;

        var vectorStore = defaultVectorStore
            ?? throw new InvalidOperationException(
                "No IVectorStore available. Register one with AddVectorStore<T>().");

        var embedder = defaultTextEmbedder
            ?? throw new InvalidOperationException(
                "No ITextEmbedder available. Register one with AddTextEmbedder<T>() or AddTextEmbedderFromAI().");

        var tool = new VectorQueryTool
        {
            VectorStore = vectorStore,
            Embedder = embedder,
            IndexName = _vectorQueryToolConfig.IndexName,
            TopK = _vectorQueryToolConfig.TopK,
            Filter = _vectorQueryToolConfig.Filter,
            Description = _vectorQueryToolConfig.ToolDescription ?? $"Search the '{_vectorQueryToolConfig.IndexName}' knowledge base for relevant information",
            Logger = loggerFactory?.CreateLogger<VectorQueryTool>()
        };

        _tools ??= new Dictionary<string, ITool>();
        _tools.TryAdd(tool.Name, tool);
    }

    private sealed class DelegateTool(string name, string description, Delegate handler) : ITool
    {
        public string Name => name;
        public string? Description => description;
        public Delegate GetDelegate() => handler;
    }
}

internal sealed record VectorQueryToolConfig(string IndexName, string? ToolDescription, int TopK = 5, IReadOnlyDictionary<string, object?>? Filter = null);
