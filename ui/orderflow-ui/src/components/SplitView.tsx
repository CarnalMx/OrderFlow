import type { ReactNode } from "react";
import "./SplitView.css";

export function SplitView(props: { left: ReactNode; right: ReactNode }) {
  return (
    <div className="splitView">
      <section className="splitLeft">{props.left}</section>
      <section className="splitRight">{props.right}</section>
    </div>
  );
}
