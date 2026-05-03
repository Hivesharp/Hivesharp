using Hivesharp.Storage.Postgres;
using Npgsql;
using Xunit;

namespace Hivesharp.IntegrationTests.Storage.Postgres;

[Collection("postgres")]
public class PostgresSchemaInitializerTests(PostgresFixture fixture)
{
    private async Task<bool> TableExists(string schema, string tableUnqualified)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = @schema AND table_name = @table
            );
            """;
        await using var conn = await fixture.DataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", tableUnqualified);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<bool> ExtensionInstalled(string name)
    {
        await using var conn = await fixture.DataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_extension WHERE extname = @n);", conn);
        cmd.Parameters.AddWithValue("n", name);
        return (bool)(await cmd.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task StartAsync_Creates_All_Tables_And_Vector_Extension()
    {
        var (tables, _) = await fixture.InitializeSchemaAsync();

        Assert.True(await TableExists(tables.Schema, tables.ThreadsTableUnqualified()));
        Assert.True(await TableExists(tables.Schema, tables.MessagesTableUnqualified()));
        Assert.True(await TableExists(tables.Schema, tables.WorkingMemoryTableUnqualified()));
        Assert.True(await TableExists(tables.Schema, tables.WorkflowRunsTableUnqualified()));
        Assert.True(await ExtensionInstalled("vector"));
    }

    [Fact]
    public async Task StartAsync_Is_Idempotent()
    {
        var (tables, opts) = await fixture.InitializeSchemaAsync();

        // Run again — must not throw
        var initializer2 = new PostgresSchemaInitializer(fixture.DataSource, tables, opts);
        await initializer2.StartAsync(CancellationToken.None);

        Assert.True(await TableExists(tables.Schema, tables.ThreadsTableUnqualified()));
    }

    [Fact]
    public async Task StartAsync_With_AutoInitializeSchema_False_Skips_Ddl()
    {
        var prefix = PostgresFixture.NewPrefix();
        var opts = new PostgresStorageOptions { TablePrefix = prefix, AutoInitializeSchema = false };
        var tables = new PostgresTableBuilder(opts.Schema, opts.TablePrefix);
        var initializer = new PostgresSchemaInitializer(fixture.DataSource, tables, opts);

        await initializer.StartAsync(CancellationToken.None);

        Assert.False(await TableExists(tables.Schema, tables.ThreadsTableUnqualified()));
    }
}
