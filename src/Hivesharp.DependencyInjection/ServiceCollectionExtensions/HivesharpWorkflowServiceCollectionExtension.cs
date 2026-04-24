using Hivesharp.Abstractions.Workflow;
using Hivesharp.Workflow;
using Hivesharp.DependencyInjection.Registrations;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class HivesharpWorkflowServiceCollectionExtension
{
    public static IServiceCollection AddWorkflow(this IServiceCollection services, IWorkflow workflow)
    {
        services.AddSingleton<IWorkflowRegistration>(new WorkflowRegistration(workflow));
        return services;
    }

    public static IServiceCollection AddWorkflow(this IServiceCollection services, Func<IServiceProvider, IWorkflow> factory)
    {
        services.AddSingleton<IWorkflowRegistration>(sp => new WorkflowRegistration(factory(sp)));
        return services;
    }

    public static IServiceCollection AddWorkflowRunStore<TStore>(this IServiceCollection services)
        where TStore : class, IWorkflowRunStore
    {
        services.AddSingleton<IWorkflowRunStore, TStore>();
        return services;
    }
}