using Hivesharp.Abstractions.Hive;
using Hivesharp.Abstractions.Memory;
using Hivesharp.Abstractions.Rag;
using Hivesharp.Abstractions.Workflow;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Hivesharp.DependencyInjection.Tests;

public class GenericRegistrationOverloadsTests
{
    public class FakeMemoryStorage : IMemoryStorage
    {
        public Task<MemoryThread> CreateThreadAsync(string? resourceId = null, string? title = null, CancellationToken ct = default)
            => Task.FromResult(new MemoryThread { Id = "t" });
        public Task<MemoryThread?> GetThreadAsync(string threadId, CancellationToken ct = default) => Task.FromResult<MemoryThread?>(null);
        public Task<IReadOnlyList<MemoryThread>> GetThreadsByResourceAsync(string resourceId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MemoryThread>>([]);
        public Task DeleteThreadAsync(string threadId, CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveMessagesAsync(string threadId, IReadOnlyList<MemoryMessage> messages, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<MemoryMessage>> GetMessagesAsync(string threadId, int? limit = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MemoryMessage>>([]);
        public Task<string?> GetWorkingMemoryAsync(string threadId, CancellationToken ct = default) => Task.FromResult<string?>(null);
        public Task SaveWorkingMemoryAsync(string threadId, string content, CancellationToken ct = default) => Task.CompletedTask;
    }

    public class FakeVectorStore : IVectorStore
    {
        public Task CreateIndexAsync(string indexName, int dimensions, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> HasIndexAsync(string indexName, CancellationToken ct = default) => Task.FromResult(false);
        public Task DeleteIndexAsync(string indexName, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpsertAsync(string indexName, IReadOnlyList<VectorRecord> records, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<VectorSearchResult>> QueryAsync(string indexName, float[] queryEmbedding, int topK = 10, IReadOnlyDictionary<string, object?>? filter = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<VectorSearchResult>>([]);
        public Task DeleteAsync(string indexName, IReadOnlyList<string> ids, CancellationToken ct = default) => Task.CompletedTask;
    }

    public class FakeEmbedder : ITextEmbedder
    {
        public Task<float[]> EmbedAsync(string text, CancellationToken ct = default) => Task.FromResult(new float[1]);
        public Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<float[]>>([]);
    }

    public class FakeWorkflow : IWorkflow
    {
        public WorkflowDescriptor Descriptor { get; } = new() { Id = "wf" };
        public Task<WorkflowResult> ExecuteAsync(object? input = null, CancellationToken ct = default)
            => Task.FromResult(new WorkflowResult { Status = WorkflowStatus.Completed });
        public Task<WorkflowResult> ResumeAsync(string runId, object? resumeData = null, CancellationToken ct = default)
            => Task.FromResult(new WorkflowResult { Status = WorkflowStatus.Completed });
    }

    public class FakeWorkflowRunStore : IWorkflowRunStore
    {
        public Task SaveSnapshotAsync(WorkflowSnapshot snapshot, CancellationToken ct = default) => Task.CompletedTask;
        public Task<WorkflowSnapshot?> GetSnapshotAsync(string runId, CancellationToken ct = default) => Task.FromResult<WorkflowSnapshot?>(null);
        public Task DeleteSnapshotAsync(string runId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<WorkflowSnapshot>> GetSnapshotsByWorkflowAsync(string workflowId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<WorkflowSnapshot>>([]);
    }

    [Fact]
    public void AddMemoryStorage_Type_Registers_Singleton()
    {
        var sp = new ServiceCollection().AddMemoryStorage(typeof(FakeMemoryStorage)).BuildServiceProvider();
        Assert.IsType<FakeMemoryStorage>(sp.GetRequiredService<IMemoryStorage>());
    }

    [Fact]
    public void AddMemoryStorage_Type_Invalid_Throws()
        => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddMemoryStorage(typeof(string)));

    [Fact]
    public void AddVectorStore_Type_Registers_Singleton()
    {
        var sp = new ServiceCollection().AddVectorStore(typeof(FakeVectorStore)).BuildServiceProvider();
        Assert.IsType<FakeVectorStore>(sp.GetRequiredService<IVectorStore>());
    }

    [Fact]
    public void AddVectorStore_Type_Invalid_Throws()
        => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddVectorStore(typeof(string)));

    [Fact]
    public void AddTextEmbedder_Type_Registers_Singleton()
    {
        var sp = new ServiceCollection().AddTextEmbedder(typeof(FakeEmbedder)).BuildServiceProvider();
        Assert.IsType<FakeEmbedder>(sp.GetRequiredService<ITextEmbedder>());
    }

    [Fact]
    public void AddTextEmbedder_Type_Invalid_Throws()
        => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddTextEmbedder(typeof(string)));

    [Fact]
    public void AddWorkflow_Generic_Registers_Workflow_In_Hive()
    {
        var sp = new ServiceCollection().AddHivesharp().AddWorkflow<FakeWorkflow>().BuildServiceProvider();
        var hive = sp.GetRequiredService<IHive>();
        hive.Initialize();

        var descriptor = Assert.Single(hive.GetWorkflows());
        Assert.Equal("wf", descriptor.Id);
        Assert.IsType<FakeWorkflow>(hive.GetWorkflowById("wf"));
    }

    [Fact]
    public void AddWorkflow_Type_Registers_Workflow_In_Hive()
    {
        var sp = new ServiceCollection().AddHivesharp().AddWorkflow(typeof(FakeWorkflow)).BuildServiceProvider();
        var hive = sp.GetRequiredService<IHive>();
        hive.Initialize();

        Assert.IsType<FakeWorkflow>(hive.GetWorkflowById("wf"));
    }

    [Fact]
    public void AddWorkflow_Type_Invalid_Throws()
        => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddWorkflow(typeof(string)));

    [Fact]
    public void AddWorkflowRunStore_Type_Registers_Singleton()
    {
        var sp = new ServiceCollection().AddWorkflowRunStore(typeof(FakeWorkflowRunStore)).BuildServiceProvider();
        Assert.IsType<FakeWorkflowRunStore>(sp.GetRequiredService<IWorkflowRunStore>());
    }

    [Fact]
    public void AddWorkflowRunStore_Type_Invalid_Throws()
        => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddWorkflowRunStore(typeof(string)));
}
