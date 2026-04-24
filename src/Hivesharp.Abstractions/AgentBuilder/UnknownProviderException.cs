namespace Hivesharp.Abstractions.AgentBuilder;

public sealed class UnknownProviderException : InvalidOperationException
{
    public UnknownProviderException(string providerName, IEnumerable<string> registeredProviders)
        : base(BuildMessage(providerName, registeredProviders, out var list))
    {
        ProviderName = providerName;
        RegisteredProviders = list;
    }

    public string ProviderName { get; }
    public IReadOnlyList<string> RegisteredProviders { get; }

    private static string BuildMessage(string providerName, IEnumerable<string> registeredProviders, out IReadOnlyList<string> list)
    {
        list = registeredProviders.ToList();
        var registered = list.Count == 0 ? "<none>" : string.Join(", ", list);
        return $"No IChatClientProvider registered for '{providerName}'. Registered: [{registered}]. " +
               "Register one with e.g. services.AddOpenAI(apiKey).";
    }
}
