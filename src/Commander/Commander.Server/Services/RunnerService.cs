using Commander.Core.Entities;
using Commander.Core.Ports;
using Grpc.Core;

namespace Commander.Server.Services;

public class RunnerService(ILogger<RunnerService> logger, IJobStore store) : Commander.RunnerService.RunnerServiceBase
{
  public override async Task<GetJobDetailsResponse> GetJobDetails(GetJobDetailsRequest request, ServerCallContext context)
  {
    if (logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation("Runner is requesting details for Job ID: {JobId}", request.JobId);
    }

    if (!Guid.TryParse(request.JobId, out var guid))
    {
      throw new RpcException(new Status(StatusCode.InvalidArgument, "The provided JobId is not a valid GUID."));
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

    var response = new GetJobDetailsResponse();
    response.Commands.AddRange(job.Commands);

    return response;
  }

  public override async Task<StreamLogsResponse> StreamLogs(IAsyncStreamReader<LogMessage> requestStream, ServerCallContext context)
  {
    await foreach (var response in requestStream.ReadAllAsync())
    {
      Console.Write(response.IsError ? "Error from " : "Log from ");
      Console.WriteLine($"{response.JobId}: {response.Log}");
    }

    return new StreamLogsResponse
    {
      Success = true
    };
  }
}
