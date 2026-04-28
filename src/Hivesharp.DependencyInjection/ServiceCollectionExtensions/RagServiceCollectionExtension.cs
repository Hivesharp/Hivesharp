using Hivesharp.Abstractions.Rag;
using Hivesharp.Rag;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class RagServiceCollectionExtension
{
    public static IServiceCollection AddVectorStore<TStore>(this IServiceCollection services)
        where TStore : class, IVectorStore
    {
        services.AddSingleton<IVectorStore, TStore>();
        return services;
    }

    public static IServiceCollection AddVectorStore(this IServiceCollection services, Type storeType)
    {
        if (!typeof(IVectorStore).IsAssignableFrom(storeType))
            throw new ArgumentException($"Type '{storeType.FullName}' does not implement {nameof(IVectorStore)}.", nameof(storeType));

        services.AddSingleton(typeof(IVectorStore), storeType);
        return services;
    }

    public static IServiceCollection AddTextEmbedder<TEmbedder>(this IServiceCollection services)
        where TEmbedder : class, ITextEmbedder
    {
        services.AddSingleton<ITextEmbedder, TEmbedder>();
        return services;
    }

    public static IServiceCollection AddTextEmbedder(this IServiceCollection services, Type embedderType)
    {
        if (!typeof(ITextEmbedder).IsAssignableFrom(embedderType))
            throw new ArgumentException($"Type '{embedderType.FullName}' does not implement {nameof(ITextEmbedder)}.", nameof(embedderType));

        services.AddSingleton(typeof(ITextEmbedder), embedderType);
        return services;
    }

    public static IServiceCollection AddTextEmbedderFromAI(this IServiceCollection services)
    {
        services.AddSingleton<ITextEmbedder>(sp =>
            new MicrosoftAITextEmbedder(sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()));
        return services;
    }

    public static IServiceCollection AddRagPipeline(this IServiceCollection services, Action<RagPipelineBuilder> configure)
    {
        services.AddSingleton<IRagPipeline>(sp =>
        {
            var builder = new RagPipelineBuilder(sp);
            configure(builder);
            var (vectorStore, embedder, chunkingStrategy, indexName, dimensions, chunkSize, chunkOverlap) = builder.Build();
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<RagPipeline>();
            return new RagPipeline(vectorStore, embedder, chunkingStrategy, indexName, dimensions, chunkSize, chunkOverlap, logger);
        });
        return services;
    }
}