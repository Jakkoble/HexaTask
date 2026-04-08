using Commander.Core.Factories;
using Commander.Core.Ports;
using Commander.Server.Store;
using Grpc.Core;

namespace Commander.Server.Services;

public class OrchestratorService(JobDefinitionFactory factory, IRunnerPort runnerPort, ILogger<OrchestratorService> logger, IJobStore store) : Commander.OrchestratorService.OrchestratorServiceBase
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
      job.StartRunning();

      await runnerPort.ExecuteJob(job);

      store.StoreJob(job);

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
