using Commander.Core.Entities;
using Commander.Infrastructure.Adapters;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace Commander.Infrastructure.Tests.Adapters;

public class DockerRunnerAdapterTests : IAsyncLifetime
{
  private readonly DockerRunnerAdapter _adapter;
  private readonly DockerClient _client;
  private Job? _job;

  public DockerRunnerAdapterTests()
  {
    _adapter = new DockerRunnerAdapter();
    _client = new DockerClientConfiguration(DockerRunnerAdapter.GetDockerUri())
      .CreateClient();
  }

  public Task InitializeAsync()
  {
    _job = new Job("tester", ["ls -la", "echo 'Hello World!'"]);
    return Task.CompletedTask;
  }

  public async Task DisposeAsync()
  {
    await _adapter.StopJob(_job!, false);
  }


  [Fact]
  public async Task ExecuteJob()
  {
    await _adapter.ExecuteJob(_job!);

    var expectedName = $"/hexatask-{_job!.Id}";

    var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
    var createdContainer = containers.FirstOrDefault(c => c.Names.Contains(expectedName));

    Assert.NotNull(createdContainer);
    Assert.Equal("running", createdContainer.State);

    await _adapter.StopJob(_job, true);

    containers = await _client.Containers.ListContainersAsync(new ContainersListParameters { All = true });
    var removedContainer = containers.FirstOrDefault(c => c.Names.Contains(expectedName));

    Assert.Null(removedContainer);
  }
}
