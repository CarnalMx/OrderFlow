import { useEffect, useState } from "react";

type WorkerStatusDto = {
  running: boolean;
  processedMessages: number;
  avgProcessedTimeMs: number;
  queuePending: number;
};

function StatBox(props: {
  label: string;
  value: React.ReactNode;
  full?: boolean;
  tone?: "normal" | "warn" | "danger";
}) {
  const toneStyles =
    props.tone === "danger"
      ? {
          border: "1px solid rgba(255, 80, 80, 0.45)",
          background: "rgba(255, 80, 80, 0.08)",
        }
      : props.tone === "warn"
      ? {
          border: "1px solid rgba(255, 200, 80, 0.45)",
          background: "rgba(255, 200, 80, 0.08)",
        }
      : {
          border: "1px solid rgba(255,255,255,0.12)",
          background: "rgba(255,255,255,0.04)",
        };

  return (
    <div
      style={{
        gridColumn: props.full ? "1 / -1" : undefined,
        borderRadius: 14,
        padding: "10px 12px",
        ...toneStyles,
      }}
    >
      <div style={{ fontSize: 11, opacity: 0.7 }}>{props.label}</div>
      <div style={{ fontWeight: 900, fontSize: 16, marginTop: 4 }}>
        {props.value}
      </div>
    </div>
  );
}

export function WorkerBox() {
  const [data, setData] = useState<WorkerStatusDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      setError(null);

      const res = await fetch("/worker/status");
      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      const json = (await res.json()) as WorkerStatusDto;
      setData(json);
    } catch (e) {
      setError(String(e));
    }
  }

  useEffect(() => {
    load();
    const t = setInterval(load, 1500);
    return () => clearInterval(t);
  }, []);

  // =========================
  // Debug override (UI only)
  // =========================
  const debugOn = localStorage.getItem("of_debug_on") === "true";
  const scenario = localStorage.getItem("of_debug_scenario") ?? "normal";

  const debugOverrides: Record<string, Partial<WorkerStatusDto>> = {
    normal: {},
    pending50: { queuePending: 50 },
    pending200: { queuePending: 200 },
    avg500: { avgProcessedTimeMs: 500 },
    avg2000: { avgProcessedTimeMs: 2000 },
    meltdown: { queuePending: 999, avgProcessedTimeMs: 2500, processedMessages: 99999 },
  };

  const shown: WorkerStatusDto | null =
    debugOn && data ? { ...data, ...(debugOverrides[scenario] ?? {}) } : data;

  // =========================
  // Derived UI values
  // =========================
  const running = shown?.running ?? false;
  const statusText = shown ? (running ? "RUNNING" : "STOPPED") : "—";

  const statusBorder = shown
    ? running
      ? "rgba(80, 255, 160, 0.55)"
      : "rgba(255, 80, 80, 0.55)"
    : "rgba(255,255,255,0.18)";

  const statusBg = shown
    ? running
      ? "rgba(80, 255, 160, 0.10)"
      : "rgba(255, 80, 80, 0.10)"
    : "rgba(255,255,255,0.06)";

  const pending = shown ? shown.queuePending : null;
  const processed = shown ? shown.processedMessages : null;
  const avgMs = shown ? Math.round(shown.avgProcessedTimeMs) : null;

  // pending tone thresholds
  const pendingTone: "normal" | "warn" | "danger" =
    pending === null ? "normal" : pending >= 200 ? "danger" : pending >= 50 ? "warn" : "normal";

  // avg tone thresholds
  const avgTone: "normal" | "warn" | "danger" =
    avgMs === null ? "normal" : avgMs >= 1000 ? "danger" : avgMs >= 200 ? "warn" : "normal";

  return (
    <div
      style={{
        border: "1px solid rgba(255,255,255,0.14)",
        background: "rgba(255,255,255,0.06)",
        borderRadius: 18,
        padding: 12,
      }}
    >
      {/* Header */}
      <div style={{ display: "flex", justifyContent: "space-between", gap: 12 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <div style={{ fontWeight: 900, fontSize: 13 }}>Worker</div>

          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              fontSize: 12,
              fontWeight: 900,
              padding: "5px 10px",
              borderRadius: 999,
              border: `1px solid ${statusBorder}`,
              background: statusBg,
              letterSpacing: 0.4,
            }}
          >
            <span
              style={{
                width: 8,
                height: 8,
                borderRadius: 999,
                background: shown
                  ? running
                    ? "rgba(80, 255, 160, 0.95)"
                    : "rgba(255, 80, 80, 0.95)"
                  : "rgba(255,255,255,0.40)",
              }}
            />
            {statusText}
          </div>

          {debugOn && (
            <div
              style={{
                fontSize: 11,
                fontWeight: 900,
                padding: "4px 8px",
                borderRadius: 999,
                border: "1px solid rgba(255,255,255,0.14)",
                background: "rgba(0,0,0,0.25)",
                opacity: 0.85,
              }}
            >
              DEBUG
            </div>
          )}
        </div>

        <button
          onClick={load}
          style={{
            padding: "6px 10px",
            borderRadius: 12,
            border: "1px solid rgba(255,255,255,0.14)",
            background: "rgba(0,0,0,0.25)",
            color: "rgba(255,255,255,0.92)",
            cursor: "pointer",
            fontWeight: 800,
            fontSize: 12,
          }}
        >
          Refresh
        </button>
      </div>

      {error && (
        <div style={{ marginTop: 8, color: "#ff6b6b", fontSize: 12 }}>
          {error}
        </div>
      )}

      {/* Stats */}
      <div
        style={{
          marginTop: 10,
          display: "grid",
          gridTemplateColumns: "1fr 1fr",
          gap: 10,
        }}
      >
        <StatBox label="pending" value={pending ?? "—"} tone={pendingTone} />
        <StatBox label="processed" value={processed ?? "—"} />
        <StatBox full label="avg ms" value={avgMs ?? "—"} tone={avgTone} />
      </div>
    </div>
  );
}
