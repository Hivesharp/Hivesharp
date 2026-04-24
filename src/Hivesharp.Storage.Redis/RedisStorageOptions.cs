using StackExchange.Redis;

namespace Hivesharp.Storage.Redis;

public class RedisStorageOptions
{
    public string? Configuration { get; set; }

    public ConfigurationOptions? ConfigurationOptions { get; set; }

    public string KeyPrefix { get; set; } = "hivesharp";
}
