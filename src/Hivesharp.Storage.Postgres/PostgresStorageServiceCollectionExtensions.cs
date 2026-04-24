using Hivesharp.Abstractions.Memory;
using Hivesharp.Abstractions.Rag;
using Hivesharp.Abstractions.Workflow;
using Hivesharp.Storage.Postgres;
using Microsoft.Extensions.Hosting;
using Npgsql;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class PostgresStorageServiceCollectionExtensions
{
    public static IServiceCollection AddHivesharpPostgresStorage(this IServiceCollection services, string connectionString)
        => services.AddHivesharpPostgresStorage(options => options.ConnectionString = connectionString);

    public static IServiceCollection AddHivesharpPostgresStorage(
        this IServiceCollection services, Action<PostgresStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new PostgresStorageOptions();
        configure(options);

        if (options.DataSource is null && string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException(
                "PostgresStorageOptions requires either ConnectionString or DataSource to be set.");

        services.AddSingleton(options);
        services.AddSingleton(new PostgresTableBuilder(options.Schema, options.TablePrefix));

        services.AddSingleton<NpgsqlDataSource>(_ =>
        {
            if (options.DataSource is not null)
                return options.DataSource;

            var builder = new NpgsqlDataSourceBuilder(options.ConnectionString!);
            builder.UseVector();
            return builder.Build();
        });

        services.AddHostedService<PostgresSchemaInitializer>();

        return services;
    }

    public static IServiceCollection AddPostgresVectorStore(this IServiceCollection services)
    {
        services.AddSingleton<IVectorStore, PostgresVectorStore>();
        return services;
    }

    public static IServiceCollection AddPostgresMemoryStorage(this IServiceCollection services)
    {
        services.AddSingleton<IMemoryStorage, PostgresMemoryStorage>();
        return services;
    }

    public static IServiceCollection AddPostgresWorkflowRunStore(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowRunStore, PostgresWorkflowRunStore>();
        return services;
    }
}
