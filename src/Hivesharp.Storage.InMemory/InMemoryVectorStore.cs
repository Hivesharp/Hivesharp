using System.Collections.Concurrent;
using System.Numerics.Tensors;
using Hivesharp.Abstractions.Rag;

namespace Hivesharp.Storage.InMemory;

public class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, VectorRecord>> _indexes = new();

    public Task CreateIndexAsync(string indexName, int dimensions, CancellationToken cancellationToken = default)
    {
        _indexes.TryAdd(indexName, new ConcurrentDictionary<string, VectorRecord>());
        return Task.CompletedTask;
    }

    public Task<bool> HasIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_indexes.ContainsKey(indexName));
    }

    public Task DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        _indexes.TryRemove(indexName, out _);
        return Task.CompletedTask;
    }

    public Task UpsertAsync(string indexName, IReadOnlyList<VectorRecord> records, CancellationToken cancellationToken = default)
    {
        var index = _indexes.GetOrAdd(indexName, _ => new ConcurrentDictionary<string, VectorRecord>());

        foreach (var record in records)
            index[record.Id] = record;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> QueryAsync(string indexName, float[] queryEmbedding, int topK = 10, IReadOnlyDictionary<string, object?>? filter = null, CancellationToken cancellationToken = default)
    {
        if (!_indexes.TryGetValue(indexName, out var index))
            return Task.FromResult<IReadOnlyList<VectorSearchResult>>([]);

        var results = index.Values
            .Where(record => Matches(record.Metadata, filter))
            .Select(record => new { Record = record, Score = CosineSimilarity(queryEmbedding, record.Embedding) })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new VectorSearchResult
            {
                Id = x.Record.Id,
                Text = x.Record.Text,
                Score = x.Score,
                Metadata = new Dictionary<string, object?>(x.Record.Metadata)
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    private static bool Matches(IReadOnlyDictionary<string, object?> metadata, IReadOnlyDictionary<string, object?>? filter)
    {
        if (filter is null || filter.Count == 0) return true;
        foreach (var (key, expected) in filter)
        {
            if (!metadata.TryGetValue(key, out var actual)) return false;
            if (!Equals(actual, expected)) return false;
        }
        return true;
    }

    public Task DeleteAsync(string indexName, IReadOnlyList<string> ids, CancellationToken cancellationToken = default)
    {
        if (_indexes.TryGetValue(indexName, out var index))
        {
            foreach (var id in ids)
                index.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        return TensorPrimitives.CosineSimilarity(a.AsSpan(), b.AsSpan());
    }
}
