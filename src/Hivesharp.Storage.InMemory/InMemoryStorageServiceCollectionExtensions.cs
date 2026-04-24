using Hivesharp.Abstractions.Memory;
using Hivesharp.Abstractions.Rag;
using Hivesharp.Abstractions.Workflow;
using Hivesharp.Storage.InMemory;

namespace Microsoft.Extensions.DependencyInjection;

public static class InMemoryStorageServiceCollectionExtensions
{
    public static IServiceCollection AddHivesharpInMemoryStorage(this IServiceCollection services)
    {
        services.AddInMemoryMemoryStorage();
        services.AddInMemoryVectorStore();
        services.AddInMemoryWorkflowRunStore();
        return services;
    }

    public static IServiceCollection AddInMemoryMemoryStorage(this IServiceCollection services)
    {
        services.AddSingleton<IMemoryStorage, InMemoryStorage>();
        return services;
    }

    public static IServiceCollection AddInMemoryVectorStore(this IServiceCollection services)
    {
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        return services;
    }

    public static IServiceCollection AddInMemoryWorkflowRunStore(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowRunStore, InMemoryWorkflowRunStore>();
        return services;
    }
}
