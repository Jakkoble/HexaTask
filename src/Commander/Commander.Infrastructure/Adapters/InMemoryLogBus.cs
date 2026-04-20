using System.Collections.Concurrent;
using System.Threading.Channels;
using Commander.Core.Ports;

namespace Commander.Infrastructure.Adapters;

public class InMemoryLogBus : ILogBus
{
    private readonly ConcurrentDictionary<Guid, Channel<LogEntry>> _channels = new();

    public void EnsureChannel(Guid jobId)
    {
        _ = GetOrCreateChannel(jobId);
    }

    public async Task PublishAsync(Guid jobId, LogEntry entry, CancellationToken ct = default)
    {
        var channel = GetOrCreateChannel(jobId);
        await channel.Writer.WriteAsync(entry, ct);
    }

    IAsyncEnumerable<LogEntry>? ILogBus.GetReader(Guid jobId)
    {
        if (!_channels.TryGetValue(jobId, out var channel))
        {
            return null;
        }

        return channel.Reader.ReadAllAsync();
    }

    public void Complete(Guid jobId)
    {
        if (!_channels.TryGetValue(jobId, out var ch))
        {
            return;
        }

        ch.Writer.TryComplete();
    }

    private Channel<LogEntry> GetOrCreateChannel(Guid jobId)
    {
        return _channels.GetOrAdd(
            jobId,
            Channel.CreateBounded<LogEntry>(
                new BoundedChannelOptions(1024) { SingleReader = true, SingleWriter = true }
            )
        );
    }
}
