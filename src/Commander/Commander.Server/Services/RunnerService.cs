using Commander.Core.Ports;
using Grpc.Core;

namespace Commander.Server.Services;

public class RunnerService(IRunnerCallbackPort callback) : Commander.RunnerService.RunnerServiceBase
{
    public override async Task<GetJobDetailsResponse> GetJobDetails(
        GetJobDetailsRequest request,
        ServerCallContext context
    )
    {
        if (!Guid.TryParse(request.JobId, out var guid))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid GUID."));

        var commands = await callback.GetJobDetailsAsync(guid);
        var response = new GetJobDetailsResponse();
        response.Commands.AddRange(commands);
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
                continue;
            var entry = new LogEntry(msg.Log, msg.IsError, msg.IsFinal, msg.ExitCode);
            await callback.PublishLogAsync(guid, entry, context.CancellationToken);
            if (msg.IsFinal)
                break;
        }
        return new StreamLogsResponse { Success = true };
    }
}
