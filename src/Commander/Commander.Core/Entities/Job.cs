namespace Commander.Core;

public class Job(in string name, in List<string> commands)
{
  public Guid Id { get; private set; } = Guid.NewGuid();
  public string Name { get; private set; } = name;
  public JobStatus Status { get; private set; } = JobStatus.Pending;
  public IReadOnlyList<string> Commands { get; private set; } = commands;

  public void StartRunning()
  {
    if (Status != JobStatus.Pending)
    {
      throw new InvalidJobStateException("Job has to be pending to be able to start");
    }

    Status = JobStatus.Running;
  }

  public void Finish(bool wasSuccessful)
  {
    if (Status == JobStatus.Completed || Status == JobStatus.Failed)
    {
      throw new InvalidJobStateException("Job has already finished execution!");
    }

    Status = wasSuccessful ? JobStatus.Completed : JobStatus.Failed;
  }
}

public enum JobStatus
{
  Pending,
  Running,
  Completed,
  Failed
}

public class InvalidJobStateException(string message) : Exception(message);
