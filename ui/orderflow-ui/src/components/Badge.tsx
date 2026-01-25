export function Badge(props: { text: string }) {
  return (
    <span
      style={{
        padding: "2px 8px",
        borderRadius: 999,
        border: "1px solid #ddd",
        fontSize: 12,
        opacity: 0.9,
      }}
    >
      {props.text}
    </span>
  );
}