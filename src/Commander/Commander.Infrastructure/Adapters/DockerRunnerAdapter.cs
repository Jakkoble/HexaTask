using Commander.Core.Entities;
using Commander.Core.Ports;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Commander.Infrastructure.Adapters;

public class DockerRunnerAdapter : IRunnerPort
{
  private readonly DockerClient _client;

  public DockerRunnerAdapter()
  {
    _client = new DockerClientConfiguration(GetDockerUri())
      .CreateClient();
  }

  public async Task ExecuteJob(Job job)
  {
    await _client.Images.CreateImageAsync(
      parameters: new ImagesCreateParameters
      {
        FromImage = "ubuntu",
        Tag = "latest",
      },
      authConfig: null,
      progress: new Progress<JSONMessage>()
    );

    var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters()
    {
      Image = "ubuntu:latest",
      Name = ContainerName(job)
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

  public static Uri GetDockerUri()
  {
    var envHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
    if (!string.IsNullOrEmpty(envHost))
    {
      return new Uri(envHost);
    }

    if (Environment.OSVersion.Platform == PlatformID.Unix)
    {
      var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

      var colimaPath = Path.Combine(home, ".colima", "default", "docker.sock");
      if (File.Exists(colimaPath)) return new Uri($"unix://{colimaPath}");

      var orbstackPath = Path.Combine(home, ".orbstack", "run", "docker.sock");
      if (File.Exists(orbstackPath)) return new Uri($"unix://{orbstackPath}");

      return new Uri("unix:///var/run/docker.sock");
    }

    return new Uri("npipe://./pipe/docker_engine");
  }

  private static string ContainerName(Job job) => $"hexatask-{job.Id}";
}
