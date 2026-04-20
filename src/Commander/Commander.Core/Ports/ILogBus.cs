namespace Commander.Core.Ports;

public readonly record struct LogEntry(string Log, bool IsError, bool IsFinal, int ExitCode);

public interface ILogBus
{
    void EnsureChannel(Guid jobId);
    Task PublishAsync(Guid jobId, LogEntry entry, CancellationToken ct = default);
    IAsyncEnumerable<LogEntry>? GetReader(Guid jobId);
    void Complete(Guid jobId);
}
