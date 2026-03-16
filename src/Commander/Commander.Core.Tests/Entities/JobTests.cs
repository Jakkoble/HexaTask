namespace Commander.Core.Tests;

public class JobTests
{
  [Fact]
  public void Constructor_Initialization()
  {
    var name = "My Test Job";
    var commands = new List<string> { "echo 'Hello'", "ls -la" };

    var job = new Job(name, commands);

    Assert.Equal(name, job.Name);
    Assert.Equal(2, job.Commands.Count);
    Assert.Equal(JobStatus.Pending, job.Status);
    Assert.Equal(commands[1], job.Commands[1]);
  }

  [Fact]
  public void StartRunning_ChangePendingToRunning()
  {
    var job = new Job("My Test Job", ["ls -la"]);

    job.StartRunning();

    Assert.Equal(JobStatus.Running, job.Status);
  }

  [Fact]
  public void StartRunning_CannotStartNonPendingJob()
  {
    var job = new Job("My Test Job", ["ls -la"]);
    job.StartRunning();

    Assert.Throws<InvalidJobStateException>(job.StartRunning);
  }

  [Theory]
  [InlineData(true, JobStatus.Completed)]
  [InlineData(false, JobStatus.Failed)]
  public void Finish_WithDifferentOutcome(bool wasSuccessful, JobStatus expectedStatus)
  {
    var job = new Job("My test job", ["cmd"]);
    job.StartRunning();

    job.Finish(wasSuccessful);

    Assert.Equal(expectedStatus, job.Status);
  }
}
