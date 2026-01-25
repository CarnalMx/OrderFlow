import { NavLink, Outlet } from "react-router-dom";

export function Layout() {
  return (
    <div style={{ height: "100vh", display: "flex", flexDirection: "column" }}>
      <header
        style={{
          height: 56,
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "0 16px",
          borderBottom: "1px solid #ddd",
          position: "sticky",
          top: 0,
          background: "white",
          zIndex: 10,
        }}
      >
        <div style={{ fontWeight: 700 }}>OrderFlow</div>

        <nav style={{ display: "flex", gap: 12 }}>
          <NavLink to="/orders">Orders</NavLink>
          <NavLink to="/outbox">Outbox</NavLink>
        </nav>
      </header>

      <main style={{ flex: 1, overflow: "hidden" }}>
        <Outlet />
      </main>
    </div>
  );
}
