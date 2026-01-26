export function Badge(props: { text: string }) {
  const t = (props.text ?? "").toLowerCase();

  const style =
    t === "processed" || t === "confirmed"
      ? {
          border: "1px solid rgba(80, 255, 160, 0.45)",
          background: "rgba(80, 255, 160, 0.10)",
        }
      : t === "pending" || t === "draft"
      ? {
          border: "1px solid rgba(255, 200, 80, 0.45)",
          background: "rgba(255, 200, 80, 0.10)",
        }
      : t === "dead" || t === "cancelled" || t === "canceled" || t === "error"
      ? {
          border: "1px solid rgba(255, 80, 80, 0.45)",
          background: "rgba(255, 80, 80, 0.10)",
        }
      : {
          border: "1px solid rgba(255,255,255,0.16)",
          background: "rgba(255,255,255,0.06)",
        };

  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        padding: "4px 10px",
        borderRadius: 999,
        fontSize: 12,
        fontWeight: 900,
        letterSpacing: 0.2,
        color: "rgba(255,255,255,0.88)",
        ...style,
      }}
    >
      {props.text}
    </span>
  );
}
