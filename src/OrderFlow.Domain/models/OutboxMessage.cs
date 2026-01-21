namespace OrderFlow.Domain.Models;

public class OutboxMessage
{
    public long Id { get; set; }

    public Guid MessageId { get; set; } = Guid.NewGuid();

    public string Type { get; set; } = "";
    public string PayloadJson { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime? LockedAtUtc { get; set; }
    public string? LockedBy { get; set; }
    public DateTime? LockExpiresAtUtc { get; set; }

    public int AttemptCount { get; set; } = 0;
    public DateTime? NextAttemptAtUtc { get; set; }
    public string? LastError { get; set; }

}
