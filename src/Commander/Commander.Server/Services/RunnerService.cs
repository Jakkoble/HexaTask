using System.Threading.Channels;
using Commander.Core.Entities;
using Commander.Core.Ports;
using Grpc.Core;

namespace Commander.Server.Services;

public class RunnerService(ILogger<RunnerService> logger, IJobStore store, ILogBus logBus)
    : Commander.RunnerService.RunnerServiceBase
{
    public override async Task<GetJobDetailsResponse> GetJobDetails(
        GetJobDetailsRequest request,
        ServerCallContext context
    )
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Runner is requesting details for Job ID: {JobId}",
                request.JobId
            );
        }

        if (!Guid.TryParse(request.JobId, out var guid))
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "The provided JobId is not a valid GUID.")
            );
        }

        Job job;
        try
        {
            job = store.GetJob(guid);
        }
        catch
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Job not found"));
        }

        job.StartRunning();

        var response = new GetJobDetailsResponse();
        response.Commands.AddRange(job.Commands);

        return response;
    }

    public override async Task<StreamLogsResponse> StreamLogs(
        IAsyncStreamReader<LogMessage> requestStream,
        ServerCallContext context
    )
    {
        await foreach (var msg in requestStream.ReadAllAsync(context.CancellationToken))
        {
            if (!Guid.TryParse(msg.JobId, out var guid))
            {
                continue;
            }

            var entry = new LogEntry(msg.Log, msg.IsError, msg.IsFinal, msg.ExitCode);
            await logBus.PublishAsync(guid, entry, context.CancellationToken);

            if (msg.IsFinal)
            {
                var job = store.GetJob(guid);
                job.Finish(msg.ExitCode == 0);
                logBus.Complete(guid);
                break;
            }
        }

        return new StreamLogsResponse { Success = true };
    }
}
