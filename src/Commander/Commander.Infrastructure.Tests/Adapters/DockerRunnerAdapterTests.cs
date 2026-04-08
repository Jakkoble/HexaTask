using Commander.Core.Entities;
using Commander.Infrastructure.Adapters;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using Moq;

namespace Commander.Infrastructure.Tests.Adapters;

public class DockerRunnerAdapterTests
{
  private readonly Job? _job = new("tester", ["ls -la", "echo 'Hello World!'"]);

  private static IOptions<DockerRunnerOptions> DefaultOptions =>
      Options.Create(new DockerRunnerOptions { Image = "hexatask-runner:latest" });

  private static Mock<IImageOperations> BuildImagesMock()
  {
    var mockImages = new Mock<IImageOperations>();
    mockImages
        .Setup(i => i.ListImagesAsync(
            It.IsAny<ImagesListParameters>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync([new ImagesListResponse()]);
    return mockImages;
  }

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
    mockClient.Setup(c => c.Images).Returns(BuildImagesMock().Object); // ← neu

    var adapter = new DockerRunnerAdapter(mockClient.Object, DefaultOptions);

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
}
