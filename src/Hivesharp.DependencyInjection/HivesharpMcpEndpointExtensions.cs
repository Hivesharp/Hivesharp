using Hivesharp.Mcp;
using Hivesharp.Mcp.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class HivesharpMcpEndpointExtensions
{
    public static IMcpServerBuilder AddHivesharpMcpHttpServer(
        this IServiceCollection services,
        Action<HivesharpMcpServerOptions>? configure = null)
    {
        var options = new HivesharpMcpServerOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<HivesharpMcpBootstrapper>();
        services.AddHostedService(sp => sp.GetRequiredService<HivesharpMcpBootstrapper>());

        return services.AddMcpServer(serverOptions =>
        {
            serverOptions.ServerInfo = new()
            {
                Name = options.ServerName,
                Version = options.ServerVersion
            };
        }).WithHttpTransport();
    }

    public static IEndpointConventionBuilder MapHivesharpMcpServer(
        this IEndpointRouteBuilder app,
        string pattern = "/mcp")
    {
        return app.MapMcp(pattern);
    }
}
