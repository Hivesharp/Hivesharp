using Hivesharp.Abstractions.Memory;
using Hivesharp.Memory;
using Xunit;

namespace Hivesharp.Tests.Memory;

public class WorkingMemoryProcessorTests
{
    [Fact]
    public void BuildInstructions_Includes_Current_Memory_Content()
    {
        var config = new WorkingMemoryConfiguration();
        var result = WorkingMemoryProcessor.BuildInstructions("You are helpful.", "User likes cats.", config);

        Assert.Contains("You are helpful.", result);
        Assert.Contains("## Working Memory", result);
        Assert.Contains("User likes cats.", result);
    }

    [Fact]
    public void BuildInstructions_Uses_Empty_Placeholder_When_Memory_Null()
    {
        var config = new WorkingMemoryConfiguration();
        var result = WorkingMemoryProcessor.BuildInstructions("base", null, config);
        Assert.Contains("(empty)", result);
    }

    [Fact]
    public void BuildInstructions_Appends_Config_Instructions_When_Provided()
    {
        var config = new WorkingMemoryConfiguration { Instructions = "Track user name and topic." };
        var result = WorkingMemoryProcessor.BuildInstructions("base", null, config);
        Assert.Contains("Track user name and topic.", result);
    }

    [Fact]
    public void ExtractWorkingMemory_No_Tag_Returns_Original_And_Null()
    {
        var (cleaned, mem) = WorkingMemoryProcessor.ExtractWorkingMemory("Hello world.");
        Assert.Equal("Hello world.", cleaned);
        Assert.Null(mem);
    }

    [Fact]
    public void ExtractWorkingMemory_Single_Tag_Strips_And_Returns_Content()
    {
        var input = "Answer: 42\n<working_memory>remembered fact</working_memory>";
        var (cleaned, mem) = WorkingMemoryProcessor.ExtractWorkingMemory(input);

        Assert.Equal("Answer: 42", cleaned);
        Assert.Equal("remembered fact", mem);
    }

    [Fact]
    public void ExtractWorkingMemory_Multiline_Content_Is_Preserved_After_Trim()
    {
        var input = "prefix\n<working_memory>\n  line1\n  line2\n</working_memory>\nsuffix";
        var (cleaned, mem) = WorkingMemoryProcessor.ExtractWorkingMemory(input);

        Assert.Equal("prefix\n\nsuffix", cleaned);
        Assert.Equal("line1\n  line2", mem);
    }

    [Fact]
    public void ExtractWorkingMemory_Is_Case_Sensitive()
    {
        var input = "hi <Working_Memory>foo</Working_Memory>";
        var (cleaned, mem) = WorkingMemoryProcessor.ExtractWorkingMemory(input);

        // Uppercase variant should NOT be extracted
        Assert.Null(mem);
        Assert.Equal(input, cleaned);
    }
}
