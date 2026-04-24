using Microsoft.Extensions.AI;

namespace Hivesharp.Tests.Helpers;

internal sealed class FakeChatClient : IChatClient
{
    public Func<IEnumerable<ChatMessage>, ChatOptions?, ChatResponse> Responder { get; set; }
        = (_, _) => new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"));

    public List<IEnumerable<ChatMessage>> ReceivedMessages { get; } = [];
    public List<ChatOptions?> ReceivedOptions { get; } = [];

    public void Dispose() { }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var materialized = messages.ToList();
        ReceivedMessages.Add(materialized);
        ReceivedOptions.Add(options);
        return Task.FromResult(Responder(materialized, options));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
