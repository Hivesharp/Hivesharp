using Hivesharp.Abstractions.Workflow;
using Hivesharp.Workflow;

namespace Hivesharp.DependencyInjection.Registrations;

internal class WorkflowRegistration(IWorkflow workflow) : IWorkflowRegistration
{
    public IWorkflow Workflow { get; } = workflow;
}