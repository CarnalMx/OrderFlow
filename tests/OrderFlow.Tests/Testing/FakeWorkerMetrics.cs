using OrderFlow.Application.Abstractions;

namespace OrderFlow.Tests.Testing;

public sealed class FakeWorkerMetrics : IWorkerMetrics
{
    private long _processed;
    private long _totalMs;
    private int _samples;
    private bool _running;

    public void MarkRunning(bool running) => _running = running;

    public void RecordProcessedMessage(TimeSpan elapsed)
    {
        Interlocked.Increment(ref _processed);
        Interlocked.Add(ref _totalMs, (long)elapsed.TotalMilliseconds);
        Interlocked.Increment(ref _samples);
    }

    public WorkerStatusSnapshot GetSnapshot()
    {
        var avg = _samples == 0 ? 0 : (double)_totalMs / _samples;
        return new WorkerStatusSnapshot(_running, _processed, avg);
    }
}
