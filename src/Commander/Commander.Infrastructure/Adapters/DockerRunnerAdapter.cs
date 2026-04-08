using Commander.Core.Entities;
using Commander.Core.Ports;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;

namespace Commander.Infrastructure.Adapters;

public class DockerRunnerOptions
{
  public string Image { get; set; } = "hexatask-runner:latest";
}

public class DockerRunnerAdapter(IDockerClient client, IOptions<DockerRunnerOptions> opts) : IRunnerPort
{
  private readonly IDockerClient _client = client;
  private readonly string _image = opts.Value.Image;
  private readonly bool _isImageRemote = opts.Value.Image.Contains('/');

  public async Task ExecuteJob(Job job)
  {
    await EnsureImageAsync();

    var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters()
    {
      Image = _image,
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

  private async Task EnsureImageAsync()
  {
    Console.WriteLine($"Check if image {_image} exists");

    if (_isImageRemote)
    {
      await _client.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = _image },
            authConfig: null,
            new Progress<JSONMessage>()
        );

      return;
    }

    var images = await _client.Images.ListImagesAsync(
        new ImagesListParameters
        {
          Filters = new Dictionary<string, IDictionary<string, bool>>()
          {
            ["reference"] = new Dictionary<string, bool> { [_image] = true }
          }
        }
    );

    if (images.Count == 0)
      throw new InvalidOperationException(
          $"Local runner image '{_image}' not found. Run at project root: docker build -t {_image} ./src/Runner"
      );
  }
}
