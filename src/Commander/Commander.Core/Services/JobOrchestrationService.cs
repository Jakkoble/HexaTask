using Commander.Core.Factories;
using Commander.Core.Ports;

namespace Commander.Core.Services;

public class JobOrchestrationService(
    IJobDefinitionFactory factory,
    IRunnerPort runnerPort,
    IJobStore store,
    ILogBus logBus
) : IJobOrchestrationPort
{
    public async Task<Guid> SubmitJobAsync(string yamlPayload)
    {
        var job = factory.CreateFromYaml(yamlPayload);
        if (!store.StoreJob(job))
            throw new InvalidOperationException($"Job {job.Id} already exists.");

        logBus.EnsureChannel(job.Id);
        await runnerPort.ExecuteJob(job);
        return job.Id;
    }

    public IAsyncEnumerable<LogEntry>? MonitorJobAsync(Guid jobId, CancellationToken ct) =>
        logBus.GetReader(jobId);
}
