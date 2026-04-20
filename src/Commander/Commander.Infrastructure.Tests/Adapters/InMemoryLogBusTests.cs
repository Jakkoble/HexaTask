using Commander.Core.Ports;
using Commander.Infrastructure.Adapters;

namespace Commander.Infrastructure.Tests.Adapters;

public class InMemoryLogBusTests
{
    [Fact]
    public async Task EnsureChannel_AllowsReaderBeforeFirstLog()
    {
        var bus = new InMemoryLogBus();
        var jobId = Guid.NewGuid();

        bus.EnsureChannel(jobId);

        var reader = ((ILogBus)bus).GetReader(jobId);

        Assert.NotNull(reader);

        await bus.PublishAsync(jobId, new LogEntry("hello", false, false, 0));

        var enumerator = reader!.GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal("hello", enumerator.Current.Log);
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task Complete_KeepsReaderAvailableForLateSubscribers()
    {
        var bus = new InMemoryLogBus();
        var jobId = Guid.NewGuid();

        bus.EnsureChannel(jobId);
        await bus.PublishAsync(jobId, new LogEntry("done", false, true, 0));
        bus.Complete(jobId);

        var reader = ((ILogBus)bus).GetReader(jobId);

        Assert.NotNull(reader);

        var logs = new List<LogEntry>();
        await foreach (var entry in reader!)
        {
            logs.Add(entry);
        }

        Assert.Single(logs);
        Assert.Equal("done", logs[0].Log);
        Assert.True(logs[0].IsFinal);
    }
}
