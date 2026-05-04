namespace Commander.Core.Ports;

public interface IRunnerCallbackPort
{
    Task<IReadOnlyList<string>> GetJobDetailsAsync(Guid jobId);
    Task PublishLogAsync(Guid jobId, LogEntry entry, CancellationToken ct);
}
