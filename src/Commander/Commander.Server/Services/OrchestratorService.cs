using Commander.Core.Factories;
using Commander.Core.Ports;
using Grpc.Core;

namespace Commander.Server.Services;

public class OrchestratorService(
    IJobDefinitionFactory factory,
    IRunnerPort runnerPort,
    ILogger<OrchestratorService> logger,
    IJobStore store,
    ILogBus logBus
) : Commander.OrchestratorService.OrchestratorServiceBase
{
    public override async Task<SubmitJobResponse> SubmitJob(
        SubmitJobRequest request,
        ServerCallContext context
    )
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Received following YAML Payload:\n{Name}", request.YamlPayload);
        }

        try
        {
            var job = factory.CreateFromYaml(request.YamlPayload);
            if (!store.StoreJob(job))
            {
                logger.LogError("Job {JobId} already exists in store", job.Id);
                throw new RpcException(new Status(StatusCode.Internal, "Failed to store job."));
            }

            logBus.EnsureChannel(job.Id);
            await runnerPort.ExecuteJob(job);

            return new SubmitJobResponse { JobId = job.Id.ToString() };
        }
        catch (InvalidJobDefinitionException e)
        {
            logger.LogWarning("Invalid YAML submitted: {Message}", e.Message);
            throw new RpcException(new Status(StatusCode.InvalidArgument, e.Message));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unexpected error while starting job");
            throw new RpcException(new Status(StatusCode.Internal, "An internal error occured."));
        }
    }

    public override async Task MonitorJob(
        MonitorJobRequest request,
        IServerStreamWriter<MonitorJobResponse> responseStream,
        ServerCallContext context
    )
    {
        if (!Guid.TryParse(request.JobId, out var jobId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Job ID"));
        }

        IAsyncEnumerable<LogEntry>? reader = null;
        for (var i = 0; i < 20 && reader is null; i++)
        {
            reader = logBus.GetReader(jobId);
            if (reader is null)
            {
                await Task.Delay(250, context.CancellationToken);
            }
        }

        if (reader == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Job not found"));
        }

        await foreach (var entry in reader.WithCancellation(context.CancellationToken))
        {
            await responseStream.WriteAsync(
                new MonitorJobResponse
                {
                    JobId = jobId.ToString(),
                    Log = entry.Log,
                    IsError = entry.IsError,
                    IsFinal = entry.IsFinal,
                    ExitCode = entry.ExitCode,
                }
            );
        }
    }
}
