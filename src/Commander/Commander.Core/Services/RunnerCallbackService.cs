using Commander.Core.Ports;

namespace Commander.Core.Services;

public class RunnerCallbackService(IJobStore store, ILogBus logBus) : IRunnerCallbackPort
{
    public Task<IReadOnlyList<string>> GetJobDetailsAsync(Guid jobId)
    {
        var job = store.GetJob(jobId);
        job.StartRunning();
        return Task.FromResult<IReadOnlyList<string>>(job.Commands);
    }

    public async Task PublishLogAsync(Guid jobId, LogEntry entry, CancellationToken ct)
    {
        await logBus.PublishAsync(jobId, entry, ct);
        if (entry.IsFinal)
        {
            var job = store.GetJob(jobId);
            job.Finish(entry.ExitCode == 0);
            logBus.Complete(jobId);
        }
    }
}
