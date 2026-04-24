using Hivesharp.Abstractions.Agent;
using Microsoft.Extensions.AI;

namespace Hivesharp.Mcp;

internal record McpToolResolutionResult(
    List<AITool> Tools,
    IReadOnlyList<McpServerStatus> ServerStatuses);
