namespace Commander.Core.Ports;

public interface IJobOrchestrationPort
{
    Task<Guid> SubmitJobAsync(string yamlPayload);
    IAsyncEnumerable<LogEntry>? MonitorJobAsync(Guid jobId, CancellationToken ct);
}
