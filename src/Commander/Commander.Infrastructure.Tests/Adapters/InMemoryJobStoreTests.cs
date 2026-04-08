using Commander.Core.Entities;
using Commander.Infrastructure.Adapters;

namespace Commander.Infrastructure.Tests.Adapters;

public class InMemoryJobStoreTests
{
  private readonly InMemoryJobStore _store = new();

  [Fact]
  public void StoreJob_NewJob()
  {
    var job = new Job("test-job", ["echo hello"]);

    var result = _store.StoreJob(job);

    Assert.True(result);
  }

  [Fact]
  public void StoreJob_DuplicateJob()
  {
    var job = new Job("test-job", ["echo hello"]);
    _store.StoreJob(job);

    var result = _store.StoreJob(job);

    Assert.False(result);
  }

  [Fact]
  public void GetJob_ExistingJob()
  {
    var job = new Job("test-job", ["echo hello"]);
    _store.StoreJob(job);

    var result = _store.GetJob(job.Id);

    Assert.Equal(job.Id, result.Id);
    Assert.Equal(job.Name, result.Name);
  }

  [Fact]
  public void GetJob_NonExistingId()
  {
    var randomId = Guid.NewGuid();

    Assert.Throws<KeyNotFoundException>(() => _store.GetJob(randomId));
  }

  [Fact]
  public void StoreJob_MultipleJobs()
  {
    var job1 = new Job("job-1", ["echo 1"]);
    var job2 = new Job("job-2", ["echo 2"]);
    var job3 = new Job("job-3", ["echo 3"]);

    _store.StoreJob(job1);
    _store.StoreJob(job2);
    _store.StoreJob(job3);

    Assert.Equal(job1.Id, _store.GetJob(job1.Id).Id);
    Assert.Equal(job2.Id, _store.GetJob(job2.Id).Id);
    Assert.Equal(job3.Id, _store.GetJob(job3.Id).Id);
  }
}
