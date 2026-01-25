namespace OrderFlow.Application.Abstractions;

public interface IWorkerMetrics
{
    void MarkRunning(bool running);
    void RecordProcessedMessage(TimeSpan elapsed);
    WorkerStatusSnapshot GetSnapshot();
}

public record WorkerStatusSnapshot(
    bool Running,
    long ProcessedMessages,
    double AvgProcessedTimeMs);
