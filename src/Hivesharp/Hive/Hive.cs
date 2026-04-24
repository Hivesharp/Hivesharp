using Hivesharp.Abstractions.Agent;
using Hivesharp.Abstractions.Hive;
using Hivesharp.Abstractions.Workflow;
using Hivesharp.Agent;
using Hivesharp.Agent.Contracts;
using Hivesharp.Memory;
using Hivesharp.Registries;
using Hivesharp.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace Hivesharp.Hive;

internal class Hive(IAgentRegistry agentRegistry, IMemoryRegistry memoryRegistry, IWorkflowRegistry workflowRegistry, IServiceProvider serviceProvider) : IHive
{
    public IAgent GetAgentById(string agentId) => agentRegistry.GetAgent(agentId);

    public IReadOnlyList<AgentDescriptor> GetAgents() => agentRegistry.GetAgents();

    public void RegisterAgent(IAgent agent)
    {
        if (!agentRegistry.RegisterAgent(agent))
        {
            throw new HivesharpAgentNotRegisteredException();
        }
    }

    public IWorkflow GetWorkflowById(string workflowId) => workflowRegistry.GetWorkflow(workflowId);

    public IReadOnlyList<WorkflowDescriptor> GetWorkflows() => workflowRegistry.GetWorkflows();

    public void RegisterWorkflow(IWorkflow workflow) => workflowRegistry.RegisterWorkflow(workflow);

    public void Initialize()
    {
        foreach (var reg in serviceProvider.GetServices<IAgentRegistration>())
        {
            agentRegistry.RegisterAgent(reg.Agent);

            if (reg.Memory is not null)
                memoryRegistry.RegisterMemory(reg.Agent.AgentDescriptor.Id, reg.Memory);
        }

        foreach (var reg in serviceProvider.GetServices<IWorkflowRegistration>())
        {
            workflowRegistry.RegisterWorkflow(reg.Workflow);
        }
    }
}
