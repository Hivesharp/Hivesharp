namespace Hivesharp.Abstractions.Workflow;

public class StepContext(IServiceProvider services, object? resumeData = null)
{
    public IServiceProvider Services { get; } = services;
    public object? ResumeData { get; } = resumeData;
    public bool IsResuming => ResumeData is not null;

    public T GetRequiredService<T>() where T : notnull
        => (T)(Services.GetService(typeof(T))
            ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered."));
}
