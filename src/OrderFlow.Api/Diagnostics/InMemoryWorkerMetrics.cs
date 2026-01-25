using OrderFlow.Application.Abstractions;

namespace OrderFlow.Api.Diagnostics;

public class InMemoryWorkerMetrics : IWorkerMetrics
{
    private long _processed;
    private long _totalTimeMs;
    private int _samples;
    private volatile bool _running;

    public void MarkRunning(bool running) => _running = running;

    public void RecordProcessedMessage(TimeSpan elapsed)
    {
        Interlocked.Increment(ref _processed);

        var ms = (long)elapsed.TotalMilliseconds;
        Interlocked.Add(ref _totalTimeMs, ms);
        Interlocked.Increment(ref _samples);
    }

    public WorkerStatusSnapshot GetSnapshot()
    {
        var samples = Volatile.Read(ref _samples);
        var avg = samples == 0 ? 0 : (double)Volatile.Read(ref _totalTimeMs) / samples;

        return new WorkerStatusSnapshot(
            Running: _running,
            ProcessedMessages: Volatile.Read(ref _processed),
            AvgProcessedTimeMs: avg
        );
    }
}