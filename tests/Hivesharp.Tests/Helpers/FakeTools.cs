using Hivesharp.Abstractions.Tool;

namespace Hivesharp.Tests.Helpers;

public sealed class EchoTool : ITool
{
    public string Name => "echo";
    public string? Description => "Echoes the input.";
    public Delegate GetDelegate() => (string text) => $"echo:{text}";
}

public sealed class NoopTool : ITool
{
    public string Name => "noop";
    public string? Description => null;
    public Delegate GetDelegate() => () => "noop";
}

public sealed class NotATool
{
    // Deliberately does NOT implement ITool
}

public sealed class GreetingService
{
    public string Greet(string name) => $"hello,{name}";
}

public sealed class GreetingTool(GreetingService greeting) : ITool
{
    public string Name => "greet";
    public string? Description => "Greets the input.";
    public Delegate GetDelegate() => (string name) => greeting.Greet(name);
}
