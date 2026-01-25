import { useEffect, useMemo, useState } from "react";
import { SplitView } from "../components/SplitView";
import { getOutboxMessages } from "../api/outboxApi";
import type { OutboxMessageDto, OutboxStatusFilter } from "../api/outboxDtos";
import { formatUtcDate } from "../utils/format";
import { Badge } from "../components/Badge";

function getMessageStatus(m: OutboxMessageDto): "pending" | "processed" | "dead" {
  if (m.processedAtUtc) {
    if (m.lastError && m.attemptCount > 0) return "dead";
    return "processed";
  }
  return "pending";
}

export function OutboxPage() {
  const [messages, setMessages] = useState<OutboxMessageDto[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const [status, setStatus] = useState<OutboxStatusFilter>("pending");
  const [take, setTake] = useState(50);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function refresh() {
    setLoading(true);
    setError(null);

    try {
      const data = await getOutboxMessages({ status, take });
      setMessages(data);

      if (data.length > 0 && selectedId === null) {
        setSelectedId(data[0].id);
      }
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status, take]);

  const selected = useMemo(
    () => messages.find((m) => m.id === selectedId) ?? null,
    [messages, selectedId]
  );

  const counts = useMemo(() => {
    const pending = messages.filter((m) => getMessageStatus(m) === "pending").length;
    const processed = messages.filter((m) => getMessageStatus(m) === "processed").length;
    const dead = messages.filter((m) => getMessageStatus(m) === "dead").length;
    return { pending, processed, dead };
  }, [messages]);

  return (
    <SplitView
      left={
        <div style={{ padding: 16 }}>
          <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 12 }}>
            <h2 style={{ margin: 0 }}>Outbox</h2>
            <button onClick={refresh} disabled={loading}>
              Refresh
            </button>
          </div>

          <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
            <select value={status} onChange={(e) => setStatus(e.target.value as OutboxStatusFilter)}>
              <option value="pending">pending</option>
              <option value="processed">processed</option>
              <option value="dead">dead</option>
            </select>

            <input
              type="number"
              value={take}
              min={1}
              max={500}
              onChange={(e) => setTake(Number(e.target.value))}
              style={{ width: 100 }}
            />
          </div>

          {loading && <p>Loading...</p>}
          {error && <p style={{ color: "red" }}>{error}</p>}

          <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
            {messages.map((m) => (
              <li key={m.id}>
                <button
                  onClick={() => setSelectedId(m.id)}
                  style={{
                    width: "100%",
                    textAlign: "left",
                    padding: 12,
                    marginBottom: 8,
                    border: "1px solid #ddd",
                    background: m.id === selectedId ? "#f5f5f5" : "white",
                    cursor: "pointer",
                  }}
                >
                  <div style={{ display: "flex", justifyContent: "space-between", gap: 8 }}>
                    <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                    <div style={{ fontWeight: 600 }}>{m.type}</div>
                      <Badge text={getMessageStatus(m)} />
                    </div>

                    <div style={{ fontSize: 12, opacity: 0.7 }}>#{m.id}</div>
                  </div>


                  <div style={{ fontSize: 12, opacity: 0.7 }}>
                    attempts:{" "}
                    <span style={{ fontWeight: m.attemptCount > 0 ? 700 : 400 }}>
                      {m.attemptCount}
                    </span>{" "}
                    | created: {formatUtcDate(m.createdAtUtc)}
                  </div>


                  {m.lastError && (
                    <div style={{ fontSize: 12, color: "crimson", marginTop: 6 }}>
                      {m.lastError}
                    </div>
                  )}
                </button>
              </li>
            ))}
          </ul>
        </div>
      }
      right={
        <div style={{ padding: 16 }}>
          <h2 style={{ marginTop: 0 }}>Outbox Detail</h2>

          <div style={{ display: "flex", gap: 12, marginBottom: 12 }}>
            <div style={{ border: "1px solid #ddd", padding: 12 }}>
              <div style={{ fontSize: 12, opacity: 0.7 }}>Pending</div>
              <div style={{ fontWeight: 700 }}>{counts.pending}</div>
            </div>
            <div style={{ border: "1px solid #ddd", padding: 12 }}>
              <div style={{ fontSize: 12, opacity: 0.7 }}>Processed</div>
              <div style={{ fontWeight: 700 }}>{counts.processed}</div>
            </div>
            <div style={{ border: "1px solid #ddd", padding: 12 }}>
              <div style={{ fontSize: 12, opacity: 0.7 }}>Dead</div>
              <div style={{ fontWeight: 700 }}>{counts.dead}</div>
            </div>
          </div>

          {!selected && <p>Select a message...</p>}

          {selected && (
            <div style={{ border: "1px solid #ddd", padding: 16 }}>
              <p>
                <b>Status:</b> <Badge text={getMessageStatus(selected)} />
              </p>
              <p style={{ marginTop: 0 }}>
                <b>Id:</b> {selected.id}
              </p>
              <p>
                <b>Type:</b> {selected.type}
              </p>
              <p>
                <b>MessageId:</b> {selected.messageId}
              </p>
              <p>
                <b>Created:</b> {formatUtcDate(selected.createdAtUtc)}
              </p>
              <p>
                <b>Processed:</b>{" "}
                {selected.processedAtUtc ? formatUtcDate(selected.processedAtUtc) : "—"}
              </p>
              <p>
                <b>AttemptCount:</b> {selected.attemptCount}
              </p>
              <p>
                <b>NextAttemptAtUtc:</b>{" "}
                {selected.nextAttemptAtUtc ? formatUtcDate(selected.nextAttemptAtUtc) : "—"}
              </p>

              <p>
                <b>LockedBy:</b> {selected.lockedBy ?? "—"}
              </p>
              <p>
                <b>LockExpireAtUtc:</b>{" "}
                {selected.lockExpireAtUtc ? formatUtcDate(selected.lockExpireAtUtc) : "—"}
              </p>

              {selected.lastError && (
                <>
                  <h3 style={{ marginBottom: 8 }}>LastError</h3>
                  <pre
                    style={{
                      whiteSpace: "pre-wrap",
                      padding: 12,
                      border: "1px solid #eee",
                      background: "#fafafa",
                      overflowX: "auto",
                    }}
                  >
                    {selected.lastError}
                  </pre>
                </>
              )}

              <h3 style={{ marginBottom: 8 }}>Payload</h3>
              <pre
                style={{
                  whiteSpace: "pre-wrap",
                  padding: 12,
                  border: "1px solid #eee",
                  background: "#fafafa",
                  overflowX: "auto",
                }}
              >
                {selected.payloadJson}
              </pre>
            </div>
          )}
        </div>
      }
    />
  );
}
