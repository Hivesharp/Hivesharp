using Hivesharp.Abstractions.Hive;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Hivesharp.Studio;

public static class HivesharpStudioExtensions
{
    public static WebApplication UseHivesharpStudio(this WebApplication app, string basePath = "/studio")
    {
        var hive = app.Services.GetRequiredService<IHive>();
        hive.Initialize();

        HivesharpStudioEndpoints.Map(app, basePath);
        return app;
    }
}
