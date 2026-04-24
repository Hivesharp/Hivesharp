namespace Hivesharp.Abstractions.Tool;

public interface ITool
{
    string Name { get; }
    string? Description { get; }
    Delegate GetDelegate();
}