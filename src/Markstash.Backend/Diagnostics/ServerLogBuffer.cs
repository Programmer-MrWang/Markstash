using Markstash.Backend.Configuration;
using Microsoft.Extensions.Options;

namespace Markstash.Backend.Diagnostics;

internal sealed class ServerLogBuffer
{
    private readonly int _capacity;
    private readonly Queue<ServerLogEntry> _entries = new();
    private readonly object _gate = new();

    public ServerLogBuffer(IOptions<BackendOptions> options)
    {
        _capacity = options.Value.DiagnosticBufferCapacity;
    }

    public void Add(ServerLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (_gate)
        {
            while (_entries.Count >= _capacity)
            {
                _entries.Dequeue();
            }

            _entries.Enqueue(entry);
        }
    }

    public IReadOnlyList<ServerLogEntry> ReadLatest(int maximumEntries)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumEntries);

        lock (_gate)
        {
            return _entries
                .Reverse()
                .Take(maximumEntries)
                .ToArray();
        }
    }
}
