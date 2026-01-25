export interface OutboxMessageDto {
  id: number;
  messageId: string;

  type: string;
  payloadJson: string;

  createdAtUtc: string;
  processedAtUtc?: string | null;

  lockedAtUtc?: string | null;
  lockedBy?: string | null;
  lockExpireAtUtc?: string | null;

  attemptCount: number;
  nextAttemptAtUtc?: string | null;
  lastError?: string | null;
}

export type OutboxStatusFilter = "pending" | "processed" | "dead";
