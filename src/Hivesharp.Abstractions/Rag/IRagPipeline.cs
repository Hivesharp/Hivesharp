namespace Hivesharp.Abstractions.Rag;

public interface IRagPipeline
{
    RagPipelineDescriptor Descriptor { get; }
    Task IngestAsync(RagDocument document, CancellationToken cancellationToken = default);
    Task IngestManyAsync(IReadOnlyList<RagDocument> documents, CancellationToken cancellationToken = default);
}
