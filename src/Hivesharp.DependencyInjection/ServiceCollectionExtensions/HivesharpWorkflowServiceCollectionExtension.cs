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

    public static IServiceCollection AddWorkflow<TWorkflow>(this IServiceCollection services)
        where TWorkflow : class, IWorkflow
        => services.AddWorkflow(typeof(TWorkflow));

    public static IServiceCollection AddWorkflow(this IServiceCollection services, Type workflowType)
    {
        if (!typeof(IWorkflow).IsAssignableFrom(workflowType))
            throw new ArgumentException($"Type '{workflowType.FullName}' does not implement {nameof(IWorkflow)}.", nameof(workflowType));

        services.AddSingleton<IWorkflowRegistration>(sp =>
            new WorkflowRegistration((IWorkflow)ActivatorUtilities.CreateInstance(sp, workflowType)));
        return services;
    }

    public static IServiceCollection AddWorkflowRunStore<TStore>(this IServiceCollection services)
        where TStore : class, IWorkflowRunStore
    {
        services.AddSingleton<IWorkflowRunStore, TStore>();
        return services;
    }

    public static IServiceCollection AddWorkflowRunStore(this IServiceCollection services, Type storeType)
    {
        if (!typeof(IWorkflowRunStore).IsAssignableFrom(storeType))
            throw new ArgumentException($"Type '{storeType.FullName}' does not implement {nameof(IWorkflowRunStore)}.", nameof(storeType));

        services.AddSingleton(typeof(IWorkflowRunStore), storeType);
        return services;
    }
}