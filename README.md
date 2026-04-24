# Hivesharp

A lightweight, DX-first AI agent framework for .NET, built on `Microsoft.Extensions.AI`.

Hivesharp gives you a fluent builder API for composing LLM-powered agents with tool calling, memory, retrieval-augmented generation (RAG), Model Context Protocol (MCP) integration, and workflow orchestration — without pulling in a heavyweight runtime.

<!-- [![CI](https://github.com/hivesharp/hivesharp/actions/workflows/ci.yml/badge.svg)](https://github.com/hivesharp/hivesharp/actions/workflows/ci.yml) -->
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

## Features

- **Agent builder** — fluent configuration: model, instructions, tools, memory, RAG, MCP servers, step limits.
- **Pluggable LLM providers** — OpenAI and Anthropic out of the box; add your own via `IChatClientProvider`.
- **Tool calling** — declare an `ITool`, Hivesharp reflects the delegate signature, generates the JSON schema, and wires it into the agentic loop via `Microsoft.Extensions.AI`.
- **Memory** — thread-based message history plus a working-memory scratchpad maintained by the LLM. Storage is abstracted behind `IMemoryStorage`.
- **RAG** — `RagDocument` → chunk → embed → `IVectorStore`. Agents retrieve via a `VectorQueryTool` decided by the LLM.
- **Workflows** — sequential, branching, and parallel steps with suspend/resume for human-in-the-loop.
- **MCP** — both directions. Connect agents to external MCP servers (stdio, named pipe, HTTP) and expose your Hivesharp agents and workflows as an MCP server.
- **Studio** — an embedded interactive playground that mounts on any ASP.NET Core host.

## Install

```bash
dotnet add package Hivesharp
dotnet add package Hivesharp.DependencyInjection
dotnet add package Hivesharp.Providers.OpenAI
dotnet add package Hivesharp.Storage.InMemory
```

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHivesharp()
    .AddOpenAI(builder.Configuration["OpenAI:ApiKey"]!)
    .AddHivesharpInMemoryStorage()
    .AddAgent(agent => agent
        .WithId("assistant")
        .WithModel("openai:gpt-4o")
        .WithInstructions("You are a helpful assistant.")
        .WithMessageHistoryMemory(maxMessages: 40));

var app = builder.Build();
app.UseHivesharpStudio();   // optional playground at /studio
app.Run();
```

Call the agent from anywhere with `IHive`:

```csharp
var hive = app.Services.GetRequiredService<IHive>();
var agent = hive.GetAgentById("assistant");
var result = await agent.GenerateAsync("What's the capital of Poland?", threadId: "user-42");
Console.WriteLine(result.Completion);
```

## Packages

| Package                          | Description                                                                 |
| -------------------------------- | --------------------------------------------------------------------------- |
| `Hivesharp`                      | Agents, workflows, memory, RAG, MCP client/server, registries, Hive facade. |
| `Hivesharp.Abstractions`         | Public contracts — interfaces and DTOs.                                     |
| `Hivesharp.DependencyInjection`  | DI extensions: `AddHivesharp`, `AddAgent`, `AddWorkflow`, `AddRagPipeline`, MCP hooks. |
| `Hivesharp.Providers.OpenAI`     | OpenAI `IChatClientProvider` + `AddOpenAI()`.                               |
| `Hivesharp.Providers.Anthropic`  | Anthropic `IChatClientProvider` + `AddAnthropic()`.                         |
| `Hivesharp.Storage.InMemory`     | `IMemoryStorage` + `IWorkflowRunStore` for development.                     |
| `Hivesharp.Storage.Postgres`     | Postgres-backed storage.                                                    |
| `Hivesharp.Storage.Redis`        | Redis-backed storage.                                                       |
| `Hivesharp.Studio`               | Embedded interactive playground (ASP.NET endpoints + React SPA).            |

## Examples

### Tool calling

```csharp
public sealed class GetWeatherTool : ITool
{
    public string Name => "get_weather";
    public string? Description => "Returns the current weather for a city.";
    public Delegate GetDelegate() => (string city) => $"The weather in {city} is sunny, 22°C.";
}

services.AddAgent(a => a
    .WithId("weather")
    .WithModel("openai:gpt-4o")
    .WithTool(typeof(GetWeatherTool)));
```

### Workflow with human-in-the-loop

```csharp
services.AddWorkflow(sp => new WorkflowBuilder("content-approval")
    .Step(Step.Create("draft", (input, ct) => Task.FromResult(input)))
    .Then(Step.Create("review", (input, ctx, ct) =>
        ctx.IsResuming
            ? Task.FromResult(StepExecutionResult.Continue(ctx.ResumeData))
            : Task.FromResult(StepExecutionResult.Suspend(new { draft = input }))))
    .Build(sp));

// Later, resume:
await workflow.ResumeAsync(runId, new { approved = true });
```

### Connect to an MCP server

```csharp
services
    .AddHivesharp()
    .AddHivesharpMcp()
    .AddAgent(a => a
        .WithId("mcp-agent")
        .WithModel("anthropic:claude-opus-4-7")
        .WithMcpServer("calculator", "hivesharp_mcp_calculator")           // named pipe
        .WithMcpServer("converter", new Uri("http://localhost:5002/mcp"))); // HTTP
```

### Expose Hivesharp as an MCP server (HTTP)

```csharp
builder.Services.AddHivesharpMcpHttpServer(o =>
{
    o.ServerName    = "my-hivesharp";
    o.ExposeAgents  = true;    // -> ask_{agentId} tools
    o.ExposeWorkflows = true;  // -> run_{workflowId} tools
});

app.MapHivesharpMcpServer("/mcp");
```

## Requirements

- .NET 10.0 SDK
- Node.js 20+ (only required when building `Hivesharp.Studio` from source — the embedded client is rebuilt before compilation)

## Build from source

```bash
dotnet restore Hivesharp.slnx
dotnet build   Hivesharp.slnx
dotnet test    Hivesharp.slnx
dotnet pack    Hivesharp.slnx -c Release --output artifacts
```

## Versioning and releases

Versions follow [SemVer 2.0](https://semver.org/). Tagging `vX.Y.Z[-suffix]` triggers the `publish.yml` workflow, which runs tests, packs all publishable projects, and pushes them to GitHub Packages. Symbol packages (`.snupkg`) are published alongside and Source Link is enabled, so step-through debugging works from any consumer.

The current line is pre-release — `0.1.0-alpha.N`. The `0.x` series is unstable and the public API may shift between alphas. Consumers must reference the package explicitly:

```xml
<PackageReference Include="Hivesharp" Version="0.1.0-alpha.1" />
```

NuGet does not resolve pre-release versions by default without an explicit version or `IncludePrerelease="true"`.

## Contributing

Issues and pull requests are welcome. Please run `dotnet test` before submitting a PR.

## License

Licensed under the [Apache License, Version 2.0](LICENSE). © Hivesharp contributors.
