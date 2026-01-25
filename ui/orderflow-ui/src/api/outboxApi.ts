import { API_BASE_URL } from "./http";
import type { OutboxMessageDto, OutboxStatusFilter } from "./outboxDtos";

export async function getOutboxMessages(params: {
  status?: OutboxStatusFilter;
  take?: number;
}): Promise<OutboxMessageDto[]> {
  const query = new URLSearchParams();

  if (params.status) query.set("status", params.status);
  if (params.take) query.set("take", String(params.take));

  const res = await fetch(`${API_BASE_URL}/outbox?${query.toString()}`);
  if (!res.ok) throw new Error("Failed to fetch outbox messages");

  return res.json();
}

export async function getOutboxMessageById(id: number): Promise<OutboxMessageDto> {
  const res = await fetch(`${API_BASE_URL}/outbox/${id}`);
  if (!res.ok) throw new Error("Failed to fetch outbox message");
  return res.json();
}
