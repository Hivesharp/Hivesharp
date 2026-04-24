using Hivesharp.Abstractions.Workflow;

namespace Hivesharp.Registries;

internal interface IWorkflowRegistry
{
    void RegisterWorkflow(IWorkflow workflow);
    IWorkflow GetWorkflow(string workflowId);
    IReadOnlyList<WorkflowDescriptor> GetWorkflows();
}
