using Hivesharp.Storage.Redis;
using Xunit;

// Pure unit tests for RedisKeyBuilder — no Docker required, kept in the unit-test project.
namespace Hivesharp.Tests.Storage.Redis;

public class RedisKeyBuilderTests
{
    [Fact]
    public void Default_Prefix_Is_Hivesharp()
    {
        var keys = new RedisKeyBuilder("hivesharp");
        Assert.Equal("hivesharp:memory:thread:abc", keys.Thread("abc"));
    }

    [Fact]
    public void Trailing_Colon_Is_Trimmed()
    {
        var keys = new RedisKeyBuilder("custom:");
        Assert.Equal("custom:memory:thread:abc", keys.Thread("abc"));
    }

    [Fact]
    public void Whitespace_Prefix_Falls_Back_To_Default()
    {
        var keys = new RedisKeyBuilder("   ");
        Assert.Equal("hivesharp:memory:thread:abc", keys.Thread("abc"));
    }

    [Fact]
    public void Empty_Prefix_Falls_Back_To_Default()
    {
        var keys = new RedisKeyBuilder("");
        Assert.Equal("hivesharp:memory:thread:abc", keys.Thread("abc"));
    }

    [Theory]
    [InlineData("p", "t1", "p:memory:thread:t1")]
    [InlineData("p", "abc-def", "p:memory:thread:abc-def")]
    public void Thread_Key_Format(string prefix, string id, string expected)
        => Assert.Equal(expected, new RedisKeyBuilder(prefix).Thread(id));

    [Fact]
    public void ThreadMessages_Key_Format()
        => Assert.Equal("p:memory:thread:t1:messages", new RedisKeyBuilder("p").ThreadMessages("t1"));

    [Fact]
    public void ThreadWorkingMemory_Key_Format()
        => Assert.Equal("p:memory:thread:t1:working", new RedisKeyBuilder("p").ThreadWorkingMemory("t1"));

    [Fact]
    public void ResourceThreadsIndex_Key_Format()
        => Assert.Equal("p:memory:resource:r1:threads", new RedisKeyBuilder("p").ResourceThreadsIndex("r1"));

    [Fact]
    public void WorkflowRun_Key_Format()
        => Assert.Equal("p:workflow:run:r1", new RedisKeyBuilder("p").WorkflowRun("r1"));

    [Fact]
    public void WorkflowRunsIndex_Key_Format()
        => Assert.Equal("p:workflow:wf1:runs", new RedisKeyBuilder("p").WorkflowRunsIndex("wf1"));
}
