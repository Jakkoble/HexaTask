using System.Collections.Concurrent;
using Commander.Core.Entities;

namespace Commander.Server.Store;

public class InMemoryJobStore(ILogger logger) : IJobStore
{
  private readonly ConcurrentDictionary<Guid, Job> _jobs = new();

  public void StoreJob(Job job)
  {
    if (_jobs.TryAdd(job.Id, job) && logger.IsEnabled(LogLevel.Information))
    {
      logger.LogInformation("Job {JobId} successfully added to store. Command count: {Count}", job.Id, job.Commands.Count);
    }
    else
    {
      logger.LogError("Failed to add job {JobId}. A job with this ID already exists.", job.Id);
    }
  }

  public Job GetJob(Guid jobId)
  {
    if (_jobs.TryGetValue(jobId, out var job))
    {
      if (logger.IsEnabled(LogLevel.Debug))
      {
        logger.LogDebug("Retrieved {Count} commands for Job {JobId}.", job.Commands.Count, jobId);
      }

      return job;
    }

    logger.LogWarning("Runner requested Job {JobId}, but it was not found in the store.", jobId);

    throw new KeyNotFoundException($"Job with ID {jobId} not found.");
  }
}
