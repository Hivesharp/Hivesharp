using Hivesharp.Abstractions.Memory;
using Hivesharp.Abstractions.Workflow;
using Hivesharp.Storage.Redis;
using StackExchange.Redis;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class RedisStorageServiceCollectionExtensions
{
    public static IServiceCollection AddHivesharpRedisStorage(this IServiceCollection services, string configuration)
        => services.AddHivesharpRedisStorage(options => options.Configuration = configuration);

    public static IServiceCollection AddHivesharpRedisStorage(this IServiceCollection services, Action<RedisStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new RedisStorageOptions();
        configure(options);

        if (options.ConfigurationOptions is null && string.IsNullOrWhiteSpace(options.Configuration))
            throw new InvalidOperationException(
                "RedisStorageOptions requires either Configuration (connection string) or ConfigurationOptions to be set.");

        services.AddSingleton(options);
        services.AddSingleton(new RedisKeyBuilder(options.KeyPrefix));
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            options.ConfigurationOptions is not null
                ? ConnectionMultiplexer.Connect(options.ConfigurationOptions)
                : ConnectionMultiplexer.Connect(options.Configuration!));

        return services;
    }

    public static IServiceCollection AddRedisMemoryStorage(this IServiceCollection services)
    {
        services.AddSingleton<IMemoryStorage, RedisMemoryStorage>();
        return services;
    }

    public static IServiceCollection AddRedisWorkflowRunStore(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowRunStore, RedisWorkflowRunStore>();
        return services;
    }
}
