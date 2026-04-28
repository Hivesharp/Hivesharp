using Microsoft.Extensions.Logging;

namespace Hivesharp.Diagnostics;

internal static partial class RagLog
{
    [LoggerMessage(EventId = 4001, Level = LogLevel.Information,
        Message = "RAG ingest started (index={IndexName}, sourceLen={SourceLength})")]
    public static partial void IngestStarted(ILogger logger, string indexName, int sourceLength);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Debug,
        Message = "RAG chunked (index={IndexName}, chunkCount={ChunkCount})")]
    public static partial void Chunked(ILogger logger, string indexName, int chunkCount);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Debug,
        Message = "RAG embedded (index={IndexName}, embeddingCount={EmbeddingCount}, durationMs={DurationMs})")]
    public static partial void Embedded(ILogger logger, string indexName, int embeddingCount, long durationMs);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Information,
        Message = "RAG ingest completed (index={IndexName}, chunks={ChunkCount}, totalDurationMs={DurationMs})")]
    public static partial void IngestCompleted(ILogger logger, string indexName, int chunkCount, long durationMs);

    [LoggerMessage(EventId = 4010, Level = LogLevel.Information,
        Message = "RAG ingest-many completed (index={IndexName}, documents={DocumentCount}, durationMs={DurationMs})")]
    public static partial void IngestManyCompleted(ILogger logger, string indexName, int documentCount, long durationMs);

    [LoggerMessage(EventId = 4020, Level = LogLevel.Debug,
        Message = "RAG vector query (index={IndexName}, topK={TopK}, queryLen={QueryLength}, results={ResultCount})")]
    public static partial void VectorQuery(ILogger logger, string indexName, int topK, int queryLength, int resultCount);
}
