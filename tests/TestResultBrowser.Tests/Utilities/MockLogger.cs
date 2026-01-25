using Microsoft.Extensions.Logging;

namespace TestResultBrowser.Tests.Utilities;

/// <summary>
/// Mock logger for testing - shared across all test files
/// </summary>
public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // No-op for testing
    }
}
