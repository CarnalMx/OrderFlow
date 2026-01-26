import { useEffect, useMemo, useState } from "react";
import { SplitView } from "../components/SplitView";
import { getOutboxMessages } from "../api/outboxApi";
import type { OutboxMessageDto, OutboxStatusFilter } from "../api/outboxDtos";
import { formatUtcDate } from "../utils/format";
import { Badge } from "../components/Badge";
import { WorkerBox } from "../components/WorkerBox";

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

  const panelStyle: React.CSSProperties = {
    border: "1px solid rgba(255,255,255,0.14)",
    background: "rgba(255,255,255,0.06)",
    borderRadius: 16,
    padding: 12,
  };

  const buttonStyle: React.CSSProperties = {
    padding: "10px 12px",
    borderRadius: 12,
    border: "1px solid rgba(255,255,255,0.14)",
    background: "rgba(255,255,255,0.06)",
    color: "rgba(255,255,255,0.92)",
    cursor: "pointer",
    fontWeight: 700,
  };

  const inputStyle: React.CSSProperties = {
    padding: "10px 12px",
    borderRadius: 12,
    border: "1px solid rgba(255,255,255,0.14)",
    background: "rgba(0,0,0,0.25)",
    color: "rgba(255,255,255,0.92)",
    outline: "none",
  };

  const listItemStyle = (selected: boolean): React.CSSProperties => ({
    width: "100%",
    textAlign: "left",
    padding: 12,
    marginBottom: 8,
    borderRadius: 14,
    border: "1px solid rgba(255,255,255,0.14)",
    background: selected ? "rgba(124, 92, 255, 0.22)" : "rgba(255,255,255,0.06)",
    cursor: "pointer",
    color: "rgba(255,255,255,0.92)",
  });

  const preStyle: React.CSSProperties = {
    whiteSpace: "pre-wrap",
    padding: 12,
    borderRadius: 14,
    border: "1px solid rgba(255,255,255,0.14)",
    background: "rgba(0,0,0,0.25)",
    color: "rgba(255,255,255,0.92)",
    overflowX: "auto",
  };

  return (
    <SplitView
      left={
        <div style={{ height: "100%", display: "flex", flexDirection: "column" }}>
          {/* TOP (scroll) */}
          <div style={{ flex: 1, overflow: "auto", padding: 16 }}>
            <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 12 }}>
              <h2 style={{ margin: 0 }}>Outbox</h2>
              <button onClick={refresh} disabled={loading} style={buttonStyle}>
                {loading ? "Loading..." : "Refresh"}
              </button>
            </div>

            <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
              <select
                value={status}
                onChange={(e) => setStatus(e.target.value as OutboxStatusFilter)}
                style={inputStyle}
              >
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
                style={{ ...inputStyle, width: 110 }}
              />
            </div>

            {error && (
              <div
                style={{
                  marginBottom: 12,
                  padding: 12,
                  borderRadius: 14,
                  border: "1px solid rgba(255, 0, 0, 0.35)",
                  background: "rgba(255, 0, 0, 0.08)",
                  color: "rgba(255,255,255,0.92)",
                }}
              >
                {error}
              </div>
            )}

            <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
              {messages.map((m) => (
                <li key={m.id}>
                  <button
                    onClick={() => setSelectedId(m.id)}
                    style={listItemStyle(m.id === selectedId)}
                  >
                    <div style={{ display: "flex", justifyContent: "space-between", gap: 8 }}>
                      <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                        <div style={{ fontWeight: 800 }}>{m.type}</div>
                        <Badge text={getMessageStatus(m)} />
                      </div>

                      <div style={{ fontSize: 12, opacity: 0.75 }}>#{m.id}</div>
                    </div>

                    <div style={{ fontSize: 12, opacity: 0.75, marginTop: 4 }}>
                      attempts:{" "}
                      <span style={{ fontWeight: m.attemptCount > 0 ? 800 : 500 }}>
                        {m.attemptCount}
                      </span>{" "}
                      | created: {formatUtcDate(m.createdAtUtc)}
                    </div>

                    {m.lastError && (
                      <div style={{ fontSize: 12, color: "#ff6b6b", marginTop: 6 }}>
                        {m.lastError}
                      </div>
                    )}
                  </button>
                </li>
              ))}
            </ul>
          </div>

          {/* BOTTOM (static) */}
          <div
            style={{
              padding: 12,
              borderTop: "1px solid rgba(255,255,255,0.12)",
              background: "rgba(0,0,0,0.10)",
            }}
          >
            <WorkerBox />
          </div>
        </div>
      }
      right={
        <div style={{ padding: 16 }}>
          <h2 style={{ marginTop: 0 }}>Outbox Detail</h2>

          <div style={{ display: "flex", gap: 12, marginBottom: 12, flexWrap: "wrap" }}>
            <div style={{ ...panelStyle, minWidth: 140 }}>
              <div style={{ fontSize: 12, opacity: 0.75 }}>Pending</div>
              <div style={{ fontWeight: 900, fontSize: 18 }}>{counts.pending}</div>
            </div>

            <div style={{ ...panelStyle, minWidth: 140 }}>
              <div style={{ fontSize: 12, opacity: 0.75 }}>Processed</div>
              <div style={{ fontWeight: 900, fontSize: 18 }}>{counts.processed}</div>
            </div>

            <div style={{ ...panelStyle, minWidth: 140 }}>
              <div style={{ fontSize: 12, opacity: 0.75 }}>Dead</div>
              <div style={{ fontWeight: 900, fontSize: 18 }}>{counts.dead}</div>
            </div>
          </div>

          {!selected && (
            <div style={panelStyle}>
              <p style={{ margin: 0, opacity: 0.8 }}>Select a message...</p>
            </div>
          )}

          {selected && (
            <div style={{ ...panelStyle, padding: 16 }}>
              <div style={{ display: "flex", justifyContent: "space-between", gap: 12 }}>
                <div>
                  <div style={{ opacity: 0.75, fontSize: 12 }}>Status</div>
                  <div style={{ marginTop: 6 }}>
                    <Badge text={getMessageStatus(selected)} />
                  </div>
                </div>

                <div style={{ textAlign: "right" }}>
                  <div style={{ opacity: 0.75, fontSize: 12 }}>Id</div>
                  <div style={{ fontWeight: 900 }}>#{selected.id}</div>
                </div>
              </div>

              <div style={{ marginTop: 14, display: "grid", gap: 8 }}>
                <div>
                  <b>Type:</b> {selected.type}
                </div>
                <div>
                  <b>MessageId:</b> {selected.messageId}
                </div>
                <div>
                  <b>Created:</b> {formatUtcDate(selected.createdAtUtc)}
                </div>
                <div>
                  <b>Processed:</b>{" "}
                  {selected.processedAtUtc ? formatUtcDate(selected.processedAtUtc) : "—"}
                </div>
                <div>
                  <b>AttemptCount:</b> {selected.attemptCount}
                </div>
                <div>
                  <b>NextAttemptAtUtc:</b>{" "}
                  {selected.nextAttemptAtUtc ? formatUtcDate(selected.nextAttemptAtUtc) : "—"}
                </div>
                <div>
                  <b>LockedBy:</b> {selected.lockedBy ?? "—"}
                </div>
                <div>
                  <b>LockExpireAtUtc:</b>{" "}
                  {selected.lockExpireAtUtc ? formatUtcDate(selected.lockExpireAtUtc) : "—"}
                </div>
              </div>

              {selected.lastError && (
                <div style={{ marginTop: 16 }}>
                  <h3 style={{ marginBottom: 8 }}>LastError</h3>
                  <pre style={preStyle}>{selected.lastError}</pre>
                </div>
              )}

              <div style={{ marginTop: 16 }}>
                <h3 style={{ marginBottom: 8 }}>Payload</h3>
                <pre style={preStyle}>{selected.payloadJson}</pre>
              </div>
            </div>
          )}
        </div>
      }
    />
  );
}
