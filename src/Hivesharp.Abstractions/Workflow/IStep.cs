namespace Hivesharp.Abstractions.Workflow;

public interface IStep
{
    string Id { get; }
    Task<StepExecutionResult> ExecuteAsync(object? input, StepContext context, CancellationToken cancellationToken = default);
}
