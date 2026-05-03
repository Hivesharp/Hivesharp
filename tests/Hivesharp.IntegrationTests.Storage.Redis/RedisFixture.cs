using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Hivesharp.IntegrationTests.Storage.Redis;

public sealed class RedisFixture : IAsyncLifetime
{
    public RedisContainer Container { get; } = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public IConnectionMultiplexer Multiplexer { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        Multiplexer = await ConnectionMultiplexer.ConnectAsync(Container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        Multiplexer?.Dispose();
        await Container.DisposeAsync();
    }

    /// <summary>Generates a unique key prefix per test so concurrent tests can share one Redis instance.</summary>
    public static string NewPrefix() => $"hivesharp-test-{Guid.NewGuid():N}";
}

[CollectionDefinition("redis")]
public class RedisCollection : ICollectionFixture<RedisFixture>
{
}
