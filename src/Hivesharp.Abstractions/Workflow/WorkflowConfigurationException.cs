namespace Hivesharp.Abstractions.Workflow;

public class WorkflowConfigurationException : Exception
{
    public WorkflowConfigurationException(string message) : base(message) { }
    public WorkflowConfigurationException(string message, Exception inner) : base(message, inner) { }
}
