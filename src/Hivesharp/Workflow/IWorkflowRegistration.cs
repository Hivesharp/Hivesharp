using Hivesharp.Abstractions.Workflow;

namespace Hivesharp.Workflow;

internal interface IWorkflowRegistration
{
    IWorkflow Workflow { get; }
}
