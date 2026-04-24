using Hivesharp.Abstractions.AgentBuilder;
using Hivesharp.Providers.OpenAI;

namespace Microsoft.Extensions.DependencyInjection;

public static class OpenAIProviderServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAI(this IServiceCollection services, string apiKey)
        => services.AddOpenAI(o => o.ApiKey = apiKey);

    public static IServiceCollection AddOpenAI(this IServiceCollection services, Action<OpenAIProviderOptions> configure)
    {
        var options = new OpenAIProviderOptions();
        configure(options);
        services.AddSingleton(options);
        services.AddSingleton<IChatClientProvider, OpenAIChatClientProvider>();
        return services;
    }
}
