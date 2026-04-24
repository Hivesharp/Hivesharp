using Hivesharp.Abstractions.Workflow;
using Hivesharp.Workflow;

namespace Hivesharp.Registries;

internal class WorkflowRegistry : IWorkflowRegistry
{
    private readonly Dictionary<string, IWorkflow> _workflows = new();

    public void RegisterWorkflow(IWorkflow workflow)
    {
        _workflows[workflow.Descriptor.Id] = workflow;
    }

    public IWorkflow GetWorkflow(string workflowId)
    {
        return _workflows[workflowId];
    }

    public IReadOnlyList<WorkflowDescriptor> GetWorkflows()
    {
        return _workflows.Values.Select(w => w.Descriptor).ToList();
    }
}
