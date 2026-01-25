import type { ReactNode } from "react";

export function SplitView(props: { left: ReactNode; right: ReactNode }) {
  return (
    <div style={{ height: "100%", display: "flex" }}>
      <section
        style={{
          width: 420,
          borderRight: "1px solid #ddd",
          overflow: "auto",
        }}
      >
        {props.left}
      </section>

      <section style={{ flex: 1, overflow: "auto" }}>{props.right}</section>
    </div>
  );
}
