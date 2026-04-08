using Commander.Infrastructure.Adapters;

namespace Commander.Server.Services;

public class ImageWarmupService(DockerRunnerAdapter adapter, ILogger<ImageWarmupService> logger) : IHostedService
{
  public async Task StartAsync(CancellationToken cancellationToken)
  {
    logger.LogInformation("Ensuring runner image is available...");
    await adapter.EnsureImageAsync();
    logger.LogInformation("Runner image ready.");
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
