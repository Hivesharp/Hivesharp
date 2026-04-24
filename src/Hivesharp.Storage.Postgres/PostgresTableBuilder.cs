using System.Text.RegularExpressions;

namespace Hivesharp.Storage.Postgres;

internal sealed partial class PostgresTableBuilder(string schema, string prefix)
{
    private readonly string _schema = string.IsNullOrWhiteSpace(schema) ? "public" : schema;
    private readonly string _prefix = string.IsNullOrWhiteSpace(prefix) ? "hivesharp" : prefix;

    public string Schema => _schema;
    public string Prefix => _prefix;

    public string VectorTable(string indexName)
    {
        ValidateIdentifier(indexName, nameof(indexName));
        return Quoted(_schema, $"{_prefix}_vec_{indexName}");
    }

    public string VectorTableUnqualified(string indexName)
    {
        ValidateIdentifier(indexName, nameof(indexName));
        return $"{_prefix}_vec_{indexName}";
    }

    public string ThreadsTable() => Quoted(_schema, $"{_prefix}_threads");
    public string MessagesTable() => Quoted(_schema, $"{_prefix}_messages");
    public string WorkingMemoryTable() => Quoted(_schema, $"{_prefix}_working_memory");
    public string WorkflowRunsTable() => Quoted(_schema, $"{_prefix}_workflow_runs");

    public string ThreadsTableUnqualified() => $"{_prefix}_threads";
    public string MessagesTableUnqualified() => $"{_prefix}_messages";
    public string WorkingMemoryTableUnqualified() => $"{_prefix}_working_memory";
    public string WorkflowRunsTableUnqualified() => $"{_prefix}_workflow_runs";

    public string IndexName(string tableUnqualified, string suffix) => $"{tableUnqualified}_{suffix}_idx";

    private static string Quoted(string schema, string table)
        => $"\"{schema.Replace("\"", "\"\"")}\".\"{table.Replace("\"", "\"\"")}\"";

    public static void ValidateIdentifier(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Identifier must be non-empty.", parameterName);
        if (!IdentifierRegex().IsMatch(value))
            throw new ArgumentException(
                $"Identifier '{value}' contains characters outside [a-zA-Z0-9_]. " +
                "Only ASCII letters, digits and underscore are allowed (anti-injection guard).",
                parameterName);
        if (value.Length > 48)
            throw new ArgumentException(
                $"Identifier '{value}' is longer than 48 characters; pick a shorter name to stay within Postgres limits.",
                parameterName);
    }

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex IdentifierRegex();
}
