namespace Commander.Infrastructure.Configuration;

public static class DockerConfiguration
{
  public static Uri GetDockerUri()
  {
    var envHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
    if (!string.IsNullOrEmpty(envHost))
      return new Uri(envHost);

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
}
