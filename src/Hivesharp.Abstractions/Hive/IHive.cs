using Hivesharp.Abstractions.Agent;
using Hivesharp.Abstractions.Workflow;

namespace Hivesharp.Abstractions.Hive;

public interface IHive
{
    IAgent GetAgentById(string agentId);
    IReadOnlyList<AgentDescriptor> GetAgents();
    void RegisterAgent(IAgent agent);

    IWorkflow GetWorkflowById(string workflowId);
    IReadOnlyList<WorkflowDescriptor> GetWorkflows();
    void RegisterWorkflow(IWorkflow workflow);
    void Initialize();
}
