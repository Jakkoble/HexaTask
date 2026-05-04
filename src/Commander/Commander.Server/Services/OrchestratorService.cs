using Commander.Core.Factories;
using Commander.Core.Ports;
using Grpc.Core;

namespace Commander.Server.Services;

public class OrchestratorService(IJobOrchestrationPort orchestration)
    : Commander.OrchestratorService.OrchestratorServiceBase
{
    public override async Task<SubmitJobResponse> SubmitJob(
        SubmitJobRequest request,
        ServerCallContext context
    )
    {
        try
        {
            var jobId = await orchestration.SubmitJobAsync(request.YamlPayload);
            return new SubmitJobResponse { JobId = jobId.ToString() };
        }
        catch (InvalidJobDefinitionException e)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, e.Message));
        }
        catch (Exception)
        {
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
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Job ID"));

        IAsyncEnumerable<LogEntry>? reader = null;
        for (var i = 0; i < 20 && reader == null; i++)
        {
            reader = orchestration.MonitorJobAsync(jobId, context.CancellationToken);
            if (reader == null)
                await Task.Delay(250, context.CancellationToken);
        }

        if (reader == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Job not found"));

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
