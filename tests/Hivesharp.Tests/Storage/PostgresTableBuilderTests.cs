// Pure unit tests for PostgresTableBuilder — no Docker required, kept in the unit-test project.
using Hivesharp.Storage.Postgres;
using Xunit;

namespace Hivesharp.Tests.Storage.Postgres;

public class PostgresTableBuilderTests
{
    [Fact]
    public void Default_Schema_And_Prefix_When_Empty()
    {
        var builder = new PostgresTableBuilder("", "");
        Assert.Equal("public", builder.Schema);
        Assert.Equal("hivesharp", builder.Prefix);
        Assert.Equal("\"public\".\"hivesharp_threads\"", builder.ThreadsTable());
    }

    [Fact]
    public void Whitespace_Schema_Falls_Back_To_Public()
    {
        var builder = new PostgresTableBuilder("   ", "myapp");
        Assert.Equal("public", builder.Schema);
        Assert.Equal("\"public\".\"myapp_threads\"", builder.ThreadsTable());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abc_123")]
    [InlineData("_x")]
    [InlineData("AbCdEf")]
    public void ValidateIdentifier_Accepts_Valid(string id)
    {
        // Must not throw.
        PostgresTableBuilder.ValidateIdentifier(id, nameof(id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc-def")]
    [InlineData("1abc")]               // starts with digit
    [InlineData("a;b")]
    [InlineData(";DROP TABLE")]
    [InlineData("a\"b")]
    [InlineData("a' OR 1=1 --")]
    public void ValidateIdentifier_Rejects_Invalid(string id)
    {
        Assert.Throws<ArgumentException>(() => PostgresTableBuilder.ValidateIdentifier(id, nameof(id)));
    }

    [Fact]
    public void ValidateIdentifier_Rejects_TooLong()
    {
        var tooLong = new string('a', 49);
        Assert.Throws<ArgumentException>(() => PostgresTableBuilder.ValidateIdentifier(tooLong, "id"));
    }

    [Fact]
    public void Quoted_Tables_Have_Schema_And_Doublequoted_Identifier()
    {
        var b = new PostgresTableBuilder("public", "hivesharp");
        Assert.Equal("\"public\".\"hivesharp_threads\"", b.ThreadsTable());
        Assert.Equal("\"public\".\"hivesharp_messages\"", b.MessagesTable());
        Assert.Equal("\"public\".\"hivesharp_working_memory\"", b.WorkingMemoryTable());
        Assert.Equal("\"public\".\"hivesharp_workflow_runs\"", b.WorkflowRunsTable());
    }

    [Fact]
    public void Unqualified_Tables_Skip_Schema_And_Quotes()
    {
        var b = new PostgresTableBuilder("public", "hivesharp");
        Assert.Equal("hivesharp_threads", b.ThreadsTableUnqualified());
        Assert.Equal("hivesharp_messages", b.MessagesTableUnqualified());
        Assert.Equal("hivesharp_working_memory", b.WorkingMemoryTableUnqualified());
        Assert.Equal("hivesharp_workflow_runs", b.WorkflowRunsTableUnqualified());
    }

    [Fact]
    public void Quoted_Escapes_Embedded_Quotes_In_Schema()
    {
        var b = new PostgresTableBuilder("ev\"il", "hivesharp");
        Assert.Equal("\"ev\"\"il\".\"hivesharp_threads\"", b.ThreadsTable());
    }

    [Fact]
    public void IndexName_Combines_Table_And_Suffix()
    {
        var b = new PostgresTableBuilder("public", "hivesharp");
        Assert.Equal("hivesharp_threads_resource_idx", b.IndexName(b.ThreadsTableUnqualified(), "resource"));
    }

    [Fact]
    public void VectorTable_Validates_IndexName()
    {
        var b = new PostgresTableBuilder("public", "hivesharp");
        Assert.Throws<ArgumentException>(() => b.VectorTable("docs-bad"));
        Assert.Equal("\"public\".\"hivesharp_vec_docs\"", b.VectorTable("docs"));
        Assert.Equal("hivesharp_vec_docs", b.VectorTableUnqualified("docs"));
    }

    [Fact]
    public void Custom_Schema_And_Prefix_Round_Trip()
    {
        var b = new PostgresTableBuilder("tenant_a", "agent_runs");
        Assert.Equal("\"tenant_a\".\"agent_runs_threads\"", b.ThreadsTable());
        Assert.Equal("agent_runs_threads_x_idx", b.IndexName(b.ThreadsTableUnqualified(), "x"));
    }
}
