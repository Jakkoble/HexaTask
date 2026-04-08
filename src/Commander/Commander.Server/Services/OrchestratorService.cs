using Commander.Core.Factories;
using Commander.Core.Ports;
using Grpc.Core;

namespace Commander.Server.Services;

public class OrchestratorService(IJobDefinitionFactory factory, IRunnerPort runnerPort, ILogger<OrchestratorService> logger, IJobStore store) : Commander.OrchestratorService.OrchestratorServiceBase
{
  public override async Task<SubmitJobResponse> SubmitJob(SubmitJobRequest request, ServerCallContext context)
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

      job.StartRunning();
      await runnerPort.ExecuteJob(job);

      return new SubmitJobResponse
      {
        JobId = job.Id.ToString(),
      };
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
}
