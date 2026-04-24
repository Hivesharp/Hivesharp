using Hivesharp.Abstractions.Hive;
using Hivesharp.Abstractions.Memory;
using Hivesharp.Abstractions.Rag;
using Hivesharp.Abstractions.Workflow;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Hivesharp.Studio;

internal static class HivesharpStudioEndpoints
{
    private const string StudioResourceId = "studio";

    public static void Map(IEndpointRouteBuilder app, string basePath)
    {
        MapStaticFiles(app, basePath);
        MapAgentEndpoints(app, basePath);
        MapThreadEndpoints(app, basePath);
        MapWorkflowEndpoints(app, basePath);
        MapRagEndpoints(app, basePath);
    }

    private static void MapStaticFiles(IEndpointRouteBuilder app, string basePath)
    {
        var assembly = typeof(HivesharpStudioEndpoints).Assembly;
        var fileProvider = new ManifestEmbeddedFileProvider(assembly, "wwwroot");

        var indexFile = fileProvider.GetFileInfo("index.html");
        string indexHtml;
        using (var stream = indexFile.CreateReadStream())
        using (var reader = new StreamReader(stream))
        {
            indexHtml = reader.ReadToEnd();
        }
        indexHtml = indexHtml.Replace("<head>", $"<head><base href=\"{basePath}/\">");

        app.MapGet(basePath, () => Results.Content(indexHtml, "text/html"))
            .ExcludeFromDescription();

        app.MapGet($"{basePath}/assets/{{**path}}", (string path) =>
        {
            var file = fileProvider.GetFileInfo($"assets/{path}");
            if (!file.Exists) return Results.NotFound();
            return Results.Stream(file.CreateReadStream(), GetContentType(path));
        }).ExcludeFromDescription();
    }

    private static void MapAgentEndpoints(IEndpointRouteBuilder app, string basePath)
    {
        app.MapGet($"{basePath}/api/agents", (IHive hive) => hive.GetAgents())
            .ExcludeFromDescription();

        app.MapGet($"{basePath}/api/agents/{{agentId}}", (string agentId, IHive hive) =>
        {
            try
            {
                return Results.Ok(hive.GetAgentById(agentId).AgentDescriptor);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        }).ExcludeFromDescription();

        app.MapGet($"{basePath}/api/agents/{{agentId}}/status", (string agentId, IHive hive) =>
        {
            try
            {
                return Results.Ok(hive.GetAgentById(agentId).RuntimeState);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        }).ExcludeFromDescription();

        app.MapPost($"{basePath}/api/agents/{{agentId}}/generate", async (
            string agentId,
            GenerateRequest request,
            IHive hive,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var agent = hive.GetAgentById(agentId);

                var threadId = request.ThreadId;
                if (threadId is null && agent.Memory is not null)
                {
                    var thread = await agent.Memory.Storage.CreateThreadAsync(resourceId: StudioResourceId, cancellationToken: cancellationToken);
                    threadId = thread.Id;
                }

                var result = await agent.GenerateAsync(request.Message, threadId, cancellationToken);
                return Results.Ok(new
                {
                    result.Completion,
                    result.ThreadId,
                    result.Usage,
                    result.ToolCalls,
                    RuntimeState = agent.RuntimeState
                });
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).ExcludeFromDescription();

        app.MapPost($"{basePath}/api/agents/{{agentId}}/mcp/retry", async (
            string agentId,
            IHive hive,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var agent = hive.GetAgentById(agentId);
                await agent.RetryMcpAsync(cancellationToken);
                return Results.Ok(agent.RuntimeState);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).ExcludeFromDescription();
    }

    private static void MapThreadEndpoints(IEndpointRouteBuilder app, string basePath)
    {
        app.MapPost($"{basePath}/api/agents/{{agentId}}/threads", async (
            string agentId,
            IHive hive,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var memory = hive.GetAgentById(agentId).Memory;
                if (memory is null) return Results.Json(new { error = "Agent does not have memory configured." }, statusCode: 400);

                var thread = await memory.Storage.CreateThreadAsync(resourceId: StudioResourceId, cancellationToken: cancellationToken);
                return Results.Ok(thread);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).ExcludeFromDescription();

        app.MapGet($"{basePath}/api/agents/{{agentId}}/threads", async (
            string agentId,
            IHive hive,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var memory = hive.GetAgentById(agentId).Memory;
                if (memory is null) return Results.Ok(Array.Empty<MemoryThread>());

                var threads = await memory.Storage.GetThreadsByResourceAsync(StudioResourceId, cancellationToken);
                return Results.Ok(threads);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).ExcludeFromDescription();

        app.MapGet($"{basePath}/api/agents/{{agentId}}/threads/{{threadId}}/messages", async (
            string agentId,
            string threadId,
            IHive hive,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var memory = hive.GetAgentById(agentId).Memory;
                if (memory is null) return Results.Json(new { error = "Agent does not have memory configured." }, statusCode: 400);

                var messages = await memory.Storage.GetMessagesAsync(threadId, cancellationToken: cancellationToken);
                return Results.Ok(messages);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).ExcludeFromDescription();

        app.MapGet($"{basePath}/api/agents/{{agentId}}/threads/{{threadId}}/working-memory", async (
            string agentId,
            string threadId,
            IHive hive,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var memory = hive.GetAgentById(agentId).Memory;
                if (memory is null) return Results.Json(new { error = "Agent does not have memory configured." }, statusCode: 400);

                var workingMemory = await memory.Storage.GetWorkingMemoryAsync(threadId, cancellationToken);
                return Results.Ok(new { content = workingMemory });
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).ExcludeFromDescription();
    }

    private static void MapWorkflowEndpoints(IEndpointRouteBuilder app, string basePath)
    {
        app.MapGet($"{basePath}/api/workflows", (IHive hive) =>
            hive.GetWorkflows()
        ).ExcludeFromDescription();

        app.MapGet($"{basePath}/api/workflows/{{workflowId}}", (string workflowId, IHive hive) =>
        {
            try
            {
                var workflow = hive.GetWorkflowById(workflowId);
                return Results.Ok(workflow.Descriptor);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        }).ExcludeFromDescription();

        app.MapPost($"{basePath}/api/workflows/{{workflowId}}/execute", async (
            string workflowId,
            ExecuteWorkflowRequest request,
            IHive hive,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var workflow = hive.GetWorkflowById(workflowId);
                var result = await workflow.ExecuteAsync(request.Input, cancellationToken);

                return Results.Ok(MapWorkflowResult(result));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).ExcludeFromDescription();

        app.MapGet($"{basePath}/api/workflows/{{workflowId}}/runs", async (
            string workflowId,
            IWorkflowRunStore runStore,
            CancellationToken cancellationToken) =>
        {
            var snapshots = await runStore.GetSnapshotsByWorkflowAsync(workflowId, cancellationToken);
            return Results.Ok(snapshots.Select(s => new
            {
                s.RunId,
                s.WorkflowId,
                s.SuspendedAtStepId,
                s.SuspendPayload,
                s.CreatedAt
            }));
        }).ExcludeFromDescription();

        app.MapPost($"{basePath}/api/workflows/{{workflowId}}/runs/{{runId}}/resume", async (
            string workflowId,
            string runId,
            ResumeWorkflowRequest request,
            IHive hive,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var workflow = hive.GetWorkflowById(workflowId);
                var result = await workflow.ResumeAsync(runId, request.ResumeData, cancellationToken);
                return Results.Ok(MapWorkflowResult(result));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).ExcludeFromDescription();
    }

    private static void MapRagEndpoints(IEndpointRouteBuilder app, string basePath)
    {
        app.MapGet($"{basePath}/api/rag/pipelines", (IServiceProvider sp) =>
        {
            var pipeline = sp.GetService<IRagPipeline>();
            if (pipeline is null) return Results.Ok(Array.Empty<RagPipelineDescriptor>());
            return Results.Ok(new[] { pipeline.Descriptor });
        }).ExcludeFromDescription();

        app.MapPost($"{basePath}/api/rag/pipelines/{{indexName}}/ingest", async (
            string indexName,
            IngestDocumentRequest request,
            IServiceProvider sp,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var pipeline = sp.GetService<IRagPipeline>();
                if (pipeline is null)
                    return Results.Json(new { error = "No RAG pipeline registered." }, statusCode: 400);

                if (pipeline.Descriptor.IndexName != indexName)
                    return Results.Json(new { error = $"Pipeline index '{pipeline.Descriptor.IndexName}' does not match '{indexName}'." }, statusCode: 400);

                var document = (request.MimeType?.ToLowerInvariant()) switch
                {
                    "text/markdown" => RagDocument.FromMarkdown(request.Content, request.Source),
                    "text/html" => RagDocument.FromHtml(request.Content, request.Source),
                    _ => RagDocument.FromText(request.Content, request.Source)
                };

                await pipeline.IngestAsync(document, cancellationToken);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).ExcludeFromDescription();

        app.MapPost($"{basePath}/api/rag/pipelines/{{indexName}}/query", async (
            string indexName,
            QueryIndexRequest request,
            IServiceProvider sp,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var vectorStore = sp.GetService<IVectorStore>();
                var embedder = sp.GetService<ITextEmbedder>();

                if (vectorStore is null || embedder is null)
                    return Results.Json(new { error = "IVectorStore and ITextEmbedder must be registered." }, statusCode: 400);

                var embedding = await embedder.EmbedAsync(request.Query, cancellationToken);
                var results = await vectorStore.QueryAsync(indexName, embedding, request.TopK ?? 5, cancellationToken);

                return Results.Ok(results);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        }).ExcludeFromDescription();
    }

    private static object MapWorkflowResult(WorkflowResult result) => new
    {
        result.Status,
        result.RunId,
        result.Output,
        result.SuspendedStepId,
        result.SuspendPayload,
        Steps = result.Steps.Select(s => new
        {
            s.StepId,
            s.Status,
            s.Output,
            Duration = s.Duration.TotalMilliseconds
        })
    };

    private static string GetContentType(string path) => Path.GetExtension(path) switch
    {
        ".js" => "application/javascript",
        ".css" => "text/css",
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        ".ico" => "image/x-icon",
        ".json" => "application/json",
        _ => "application/octet-stream",
    };

    public record GenerateRequest(string Message, string? ThreadId = null);
    public record ExecuteWorkflowRequest(object? Input = null);
    public record ResumeWorkflowRequest(object? ResumeData = null);
    public record IngestDocumentRequest(string Content, string? Source = null, string? MimeType = null);
    public record QueryIndexRequest(string Query, int? TopK = null);
}
