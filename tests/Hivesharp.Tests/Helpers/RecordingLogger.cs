using Microsoft.Extensions.Logging;

namespace Hivesharp.Tests.Helpers;

internal sealed record LogRecord(LogLevel Level, EventId EventId, string Message, Exception? Exception);

internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<LogRecord> Records { get; } = [];

    IDisposable? ILogger.BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Records.Add(new LogRecord(logLevel, eventId, formatter(state, exception), exception));
    }
}

internal sealed class RecordingLoggerFactory : ILoggerFactory
{
    public List<LogRecord> Records { get; } = [];
    public void AddProvider(ILoggerProvider provider) { }
    public ILogger CreateLogger(string categoryName) => new CategoryLogger(Records);
    public void Dispose() { }

    private sealed class CategoryLogger(List<LogRecord> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            sink.Add(new LogRecord(logLevel, eventId, formatter(state, exception), exception));
        }
    }
}
