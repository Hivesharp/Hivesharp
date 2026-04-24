using Hivesharp.Abstractions.AgentBuilder;
using Hivesharp.Providers.Anthropic;

namespace Microsoft.Extensions.DependencyInjection;

public static class AnthropicProviderServiceCollectionExtensions
{
    public static IServiceCollection AddAnthropic(this IServiceCollection services, string apiKey)
        => services.AddAnthropic(o => o.ApiKey = apiKey);

    public static IServiceCollection AddAnthropic(this IServiceCollection services, Action<AnthropicProviderOptions> configure)
    {
        var options = new AnthropicProviderOptions();
        configure(options);
        services.AddSingleton(options);
        services.AddSingleton<IChatClientProvider, AnthropicChatClientProvider>();
        return services;
    }
}
