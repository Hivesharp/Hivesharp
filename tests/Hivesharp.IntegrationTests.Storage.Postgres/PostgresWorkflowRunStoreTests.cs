using System.Text.Json;
using Hivesharp.Abstractions.Workflow;
using Hivesharp.Storage.Postgres;
using Xunit;

namespace Hivesharp.IntegrationTests.Storage.Postgres;

[Collection("postgres")]
public class PostgresWorkflowRunStoreTests(PostgresFixture fixture)
{
    private async Task<PostgresWorkflowRunStore> NewStore()
    {
        var (tables, _) = await fixture.InitializeSchemaAsync();
        return new PostgresWorkflowRunStore(fixture.DataSource, tables);
    }

    private static WorkflowSnapshot Snapshot(string workflowId, string runId, object? payload = null) => new()
    {
        RunId = runId,
        WorkflowId = workflowId,
        CompletedSteps =
        [
            new StepResult { StepId = "s1", Status = StepStatus.Completed, Output = "ok", Duration = TimeSpan.FromMilliseconds(10) }
        ],
        SuspendedAtStepId = "s2",
        SuspendPayload = payload,
        LastOutput = "ok",
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task SaveSnapshot_Persists_And_Indexes_By_Workflow()
    {
        var store = await NewStore();
        await store.SaveSnapshotAsync(Snapshot("wf-1", "run-1"));

        var loaded = await store.GetSnapshotAsync("run-1");
        Assert.NotNull(loaded);
        Assert.Equal("wf-1", loaded!.WorkflowId);
        Assert.Equal("s2", loaded.SuspendedAtStepId);

        var byWf = await store.GetSnapshotsByWorkflowAsync("wf-1");
        Assert.Single(byWf);
    }

    [Fact]
    public async Task SaveSnapshot_Upserts_Existing_RunId()
    {
        var store = await NewStore();
        await store.SaveSnapshotAsync(Snapshot("wf-1", "run-1"));

        // Same runId, different workflow — UPSERT semantics.
        var updated = Snapshot("wf-1", "run-1");
        await store.SaveSnapshotAsync(updated);

        var loaded = await store.GetSnapshotAsync("run-1");
        Assert.NotNull(loaded);
        // Still only one row in the index.
        var all = await store.GetSnapshotsByWorkflowAsync("wf-1");
        Assert.Single(all);
    }

    [Fact]
    public async Task GetSnapshot_Returns_Null_For_Missing_Run()
    {
        var store = await NewStore();
        Assert.Null(await store.GetSnapshotAsync("does-not-exist"));
    }

    [Fact]
    public async Task GetSnapshotsByWorkflow_Returns_Newest_First()
    {
        var store = await NewStore();
        await store.SaveSnapshotAsync(Snapshot("wf-1", "run-a"));
        await Task.Delay(10);
        await store.SaveSnapshotAsync(Snapshot("wf-1", "run-b"));
        await Task.Delay(10);
        await store.SaveSnapshotAsync(Snapshot("wf-1", "run-c"));
        await store.SaveSnapshotAsync(Snapshot("wf-2", "run-x"));

        var byWf1 = await store.GetSnapshotsByWorkflowAsync("wf-1");

        Assert.Equal(3, byWf1.Count);
        Assert.Equal("run-c", byWf1[0].RunId);
        Assert.Equal("run-b", byWf1[1].RunId);
        Assert.Equal("run-a", byWf1[2].RunId);
    }

    [Fact]
    public async Task DeleteSnapshot_Removes_Row()
    {
        var store = await NewStore();
        await store.SaveSnapshotAsync(Snapshot("wf-1", "run-1"));
        await store.SaveSnapshotAsync(Snapshot("wf-1", "run-2"));

        await store.DeleteSnapshotAsync("run-1");

        Assert.Null(await store.GetSnapshotAsync("run-1"));
        var remaining = await store.GetSnapshotsByWorkflowAsync("wf-1");
        Assert.Single(remaining);
        Assert.Equal("run-2", remaining[0].RunId);
    }

    [Fact]
    public async Task SuspendPayload_RoundTrips_As_JsonElement()
    {
        var store = await NewStore();
        var payload = new { question = "Approve?", deadline = "2026-12-31" };
        await store.SaveSnapshotAsync(Snapshot("wf-1", "run-1", payload));

        var loaded = await store.GetSnapshotAsync("run-1");

        Assert.NotNull(loaded);
        Assert.IsType<JsonElement>(loaded!.SuspendPayload);
        var element = (JsonElement)loaded.SuspendPayload!;
        Assert.Equal("Approve?", element.GetProperty("question").GetString());
    }

    [Fact]
    public async Task Cancelled_Token_Throws_OperationCanceled()
    {
        var store = await NewStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.SaveSnapshotAsync(Snapshot("wf", "r"), cts.Token));
    }
}
