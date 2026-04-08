using Commander.Core.Entities;
using Commander.Infrastructure.Adapters;
using Docker.DotNet;
using Docker.DotNet.Models;
using Moq;

namespace Commander.Infrastructure.Tests.Adapters;

public class DockerRunnerAdapterTests
{
  private readonly Job? _job = new("tester", ["ls -la", "echo 'Hello World!'"]);

  [Fact]
  public async Task ExecuteJob_CallsCreateAndStart()
  {
    var mockContainers = new Mock<IContainerOperations>();
    mockContainers
        .Setup(c => c.CreateContainerAsync(
            It.IsAny<CreateContainerParameters>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new CreateContainerResponse { ID = "abc123" });

    mockContainers
        .Setup(c => c.StartContainerAsync(
            It.IsAny<string>(),
            It.IsAny<ContainerStartParameters>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

    var mockClient = new Mock<IDockerClient>();
    mockClient.Setup(c => c.Containers).Returns(mockContainers.Object);

    var adapter = new DockerRunnerAdapter(mockClient.Object);

    await adapter.ExecuteJob(_job!);

    mockContainers.Verify(c => c.CreateContainerAsync(
        It.Is<CreateContainerParameters>(p =>
            p.Env.Contains($"JOB_ID={_job!.Id}") &&
            p.Image == "hexatask-runner:latest"),
        It.IsAny<CancellationToken>()),
        Times.Once);

    mockContainers.Verify(c => c.StartContainerAsync(
        "abc123",
        It.IsAny<ContainerStartParameters>(),
        It.IsAny<CancellationToken>()),
        Times.Once);
  }

  [Fact]
  public async Task StopJob_RemovesContainerByName()
  {
    var mockContainers = new Mock<IContainerOperations>();
    mockContainers
        .Setup(c => c.RemoveContainerAsync(
            It.IsAny<string>(),
            It.IsAny<ContainerRemoveParameters>(),
            It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

    var mockClient = new Mock<IDockerClient>();
    mockClient.Setup(c => c.Containers).Returns(mockContainers.Object);

    var adapter = new DockerRunnerAdapter(mockClient.Object);

    await adapter.StopJob(_job!, true);

    mockContainers.Verify(c => c.RemoveContainerAsync(
        $"hexatask-{_job!.Id}",
        It.IsAny<ContainerRemoveParameters>(),
        It.IsAny<CancellationToken>()),
        Times.Once);
  }
}
