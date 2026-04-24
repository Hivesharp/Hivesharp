using Hivesharp.Abstractions.Workflow;

namespace Hivesharp.Workflow;

public sealed class Step : IStep
{
    public string Id { get; }
    private readonly Func<object?, StepContext, CancellationToken, Task<StepExecutionResult>> _execute;

    private Step(string id, Func<object?, StepContext, CancellationToken, Task<StepExecutionResult>> execute)
    {
        Id = id;
        _execute = execute;
    }

    public Task<StepExecutionResult> ExecuteAsync(object? input, StepContext context, CancellationToken cancellationToken = default)
        => _execute(input, context, cancellationToken);

    // Full form: input + StepContext + CancellationToken → StepExecutionResult
    public static Step Create(string id, Func<object?, StepContext, CancellationToken, Task<StepExecutionResult>> execute)
        => new(id, execute);

    // Without context: input + CancellationToken → StepExecutionResult
    public static Step Create(string id, Func<object?, CancellationToken, Task<StepExecutionResult>> execute)
        => new(id, (input, _, ct) => execute(input, ct));

    // Simple: input → StepExecutionResult
    public static Step Create(string id, Func<object?, Task<StepExecutionResult>> execute)
        => new(id, (input, _, _) => execute(input));

    // Convenience: input + StepContext + CancellationToken → object? (auto-wrapped in Continue)
    public static Step Create(string id, Func<object?, StepContext, CancellationToken, Task<object?>> execute)
        => new(id, async (input, ctx, ct) => StepExecutionResult.Continue(await execute(input, ctx, ct)));

    // Convenience: input + CancellationToken → object? (auto-wrapped in Continue)
    public static Step Create(string id, Func<object?, CancellationToken, Task<object?>> execute)
        => new(id, async (input, _, ct) => StepExecutionResult.Continue(await execute(input, ct)));

    // Convenience: input → object? (auto-wrapped in Continue)
    public static Step Create(string id, Func<object?, Task<object?>> execute)
        => new(id, async (input, _, _) => StepExecutionResult.Continue(await execute(input)));
}
