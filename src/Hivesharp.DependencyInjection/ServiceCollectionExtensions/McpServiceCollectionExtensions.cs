using Hivesharp.Mcp;
using Hivesharp.Mcp.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddHivesharpMcp(this IServiceCollection services)
    {
        services.AddSingleton<IMcpToolResolver, McpToolResolver>();
        return services;
    }

    public static IMcpServerBuilder AddHivesharpMcpServer(
        this IServiceCollection services,
        Action<HivesharpMcpServerOptions>? configure = null)
    {
        var options = new HivesharpMcpServerOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        var builder = services.AddMcpServer(serverOptions =>
        {
            serverOptions.ServerInfo = new()
            {
                Name = options.ServerName,
                Version = options.ServerVersion
            };
        });

        // Register a hosted service that adds tools after IHive is initialized
        services.AddSingleton<HivesharpMcpBootstrapper>();
        services.AddHostedService(sp => sp.GetRequiredService<HivesharpMcpBootstrapper>());

        return builder;
    }
}
