using Hivesharp.Abstractions.Hive;
using Hivesharp.Abstractions.AgentBuilder;
using Hivesharp.Abstractions.Memory;
using Hivesharp.Agent.Builder;
using Hivesharp.Agent.Contracts;
using Hivesharp.Registries;
using Hivesharp.DependencyInjection;
using Hivesharp.DependencyInjection.Registrations;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class HivesharpServiceCollectionExtension
{
    public static IServiceCollection AddHivesharp(this IServiceCollection services)
    {
        services.AddSingleton<IAgentRegistry, AgentRegistry>();
        services.AddSingleton<IMemoryRegistry, MemoryRegistry>();
        services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();
        services.AddSingleton<IHive, Hivesharp.Hive.Hive>();

        services.AddSingleton<IAgentBuilderChatClientFactory, AgentBuilderChatClientFactory>();
        services.AddTransient<IAgentBuilder, AgentBuilder>();

        return services;
    }

    public static IServiceCollection AddAgent(this IServiceCollection services, Action<IAgentBuilderSetup> setup)
    {
        services.AddSingleton<IAgentRegistration>(sp =>
        {
            var agentBuilder = sp.GetRequiredService<IAgentBuilder>();
            setup(agentBuilder);
            return new AgentRegistration(agentBuilder.Build());
        });
        return services;
    }

    public static IServiceCollection AddMemoryStorage<TStorage>(this IServiceCollection services)
        where TStorage : class, IMemoryStorage
    {
        services.AddSingleton<IMemoryStorage, TStorage>();
        return services;
    }
}