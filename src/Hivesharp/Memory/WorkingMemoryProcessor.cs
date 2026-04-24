using System.Text.RegularExpressions;
using Hivesharp.Abstractions.Memory;

namespace Hivesharp.Memory;

internal static partial class WorkingMemoryProcessor
{
    private const string WorkingMemoryTag = "working_memory";

    [GeneratedRegex(@"<working_memory>\s*([\s\S]*?)\s*</working_memory>", RegexOptions.Compiled)]
    private static partial Regex WorkingMemoryPattern();

    internal static string BuildInstructions(string baseInstructions, string? currentWorkingMemory, WorkingMemoryConfiguration config)
    {
        var workingMemoryBlock = $"""

            ## Working Memory
            You have a working memory to track important information across conversation turns.
            Current working memory:
            {currentWorkingMemory ?? "(empty)"}

            After your response, output your updated working memory inside <working_memory> tags.
            Only include information worth persisting across turns.
            """;

        if (config.Instructions is not null)
            workingMemoryBlock += $"\n{config.Instructions}";

        return baseInstructions + workingMemoryBlock;
    }

    internal static (string CleanedText, string? UpdatedMemory) ExtractWorkingMemory(string completionText)
    {
        var match = WorkingMemoryPattern().Match(completionText);

        if (!match.Success)
            return (completionText, null);

        var updatedMemory = match.Groups[1].Value.Trim();
        // Preserve any text that appears after the closing tag
        var before = completionText[..match.Index].TrimEnd();
        var after = completionText[(match.Index + match.Length)..].TrimStart();
        var cleanedText = after.Length > 0
            ? (before.Length > 0 ? before + "\n\n" + after : after)
            : before;

        return (cleanedText, updatedMemory);
    }
}
