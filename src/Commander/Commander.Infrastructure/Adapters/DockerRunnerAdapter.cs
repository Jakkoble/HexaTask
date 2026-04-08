using Commander.Core.Entities;
using Commander.Core.Ports;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Commander.Infrastructure.Adapters;

public class DockerRunnerAdapter(in IDockerClient client) : IRunnerPort
{
  private readonly IDockerClient _client = client;

  public async Task ExecuteJob(Job job)
  {
    // await _client.Images.CreateImageAsync(
    //   parameters: new ImagesCreateParameters
    //   {
    //     FromImage = "ubuntu",
    //     Tag = "latest",
    //   },
    //   authConfig: null,
    //   progress: new Progress<JSONMessage>()
    // );

    var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters()
    {
      Image = "hexatask-runner:latest",
      Name = ContainerName(job),
      Env = [
        $"JOB_ID={job.Id}",
        $"COMMANDER_URL=host.docker.internal:5271"
      ]
    });

    await _client.Containers.StartContainerAsync(response.ID, new());
  }

  public async Task StopJob(Job job, bool wasSuccessful)
  {
    var targetContainer = ContainerName(job);
    try
    {
      await _client.Containers.RemoveContainerAsync(targetContainer, new ContainerRemoveParameters
      {
        Force = true
      });
    }
    catch (DockerContainerNotFoundException)
    {
    }
  }

  private static string ContainerName(Job job) => $"hexatask-{job.Id}";
}
