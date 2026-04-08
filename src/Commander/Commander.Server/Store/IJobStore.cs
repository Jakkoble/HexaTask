using Commander.Core.Entities;

namespace Commander.Server.Store;

public interface IJobStore
{
  void StoreJob(Job job);
  Job GetJob(Guid jobId);
}
