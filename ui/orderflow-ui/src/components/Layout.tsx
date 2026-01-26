import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import "./Layout.css";

function scenarioLabel(key: string) {
  switch (key) {
    case "normal":
      return "Normal";
    case "pending50":
      return "Pending 50";
    case "pending200":
      return "Pending 200";
    case "avg500":
      return "Avg 500ms";
    case "avg2000":
      return "Avg 2000ms";
    case "meltdown":
      return "Meltdown";
    default:
      return key;
  }
}

function MenuItem(props: {
  scenarioKey: string;
  scenario: string;
  setScenario: (v: string) => void;
  close: () => void;
}) {
  const active = props.scenario === props.scenarioKey;

  return (
    <button
      onClick={() => {
        props.setScenario(props.scenarioKey);
        props.close();
      }}
      style={{
        width: "100%",
        textAlign: "left",
        padding: "10px 10px",
        borderRadius: 12,
        border: active
          ? "1px solid rgba(124, 92, 255, 0.40)"
          : "1px solid transparent",
        background: active ? "rgba(124, 92, 255, 0.18)" : "transparent",
        color: "rgba(255,255,255,0.92)",
        fontWeight: active ? 900 : 700,
        fontSize: 12,
        cursor: "pointer",
        transition: "0.15s ease",
      }}
    >
      {scenarioLabel(props.scenarioKey)}
    </button>
  );
}

export function Layout() {
  const [debugOn, setDebugOn] = useState(false);
  const [scenario, setScenario] = useState("normal");
  const [menuOpen, setMenuOpen] = useState(false);

  const menuRef = useRef<HTMLDivElement | null>(null);

  // persist debug state
  useEffect(() => {
    localStorage.setItem("of_debug_on", String(debugOn));
    if (!debugOn) setMenuOpen(false);
  }, [debugOn]);

  useEffect(() => {
    localStorage.setItem("of_debug_scenario", scenario);
  }, [scenario]);

  // close on click outside + ESC
  useEffect(() => {
    function onDocClick(e: MouseEvent) {
      if (!menuOpen) return;
      if (!menuRef.current) return;

      const target = e.target as Node;
      if (!menuRef.current.contains(target)) {
        setMenuOpen(false);
      }
    }

    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") setMenuOpen(false);
    }

    document.addEventListener("mousedown", onDocClick);
    document.addEventListener("keydown", onKeyDown);

    return () => {
      document.removeEventListener("mousedown", onDocClick);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [menuOpen]);

  return (
    <div className="appShell">
      <header className="topbar">
        <div className="brand">
          <div className="brandTitle">OrderFlow</div>
          <span className="brandBadge">demo</span>
        </div>

        <nav className="nav">
          <NavLink
            to="/orders"
            className={({ isActive }) => (isActive ? "navLink active" : "navLink")}
          >
            Orders
          </NavLink>

          <NavLink
            to="/outbox"
            className={({ isActive }) => (isActive ? "navLink active" : "navLink")}
          >
            Outbox
          </NavLink>
        </nav>

        {/* ✅ Debug controls */}
        <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
          <label
            style={{
              display: "flex",
              gap: 8,
              alignItems: "center",
              fontSize: 12,
              opacity: 0.8,
              userSelect: "none",
            }}
          >
            <input
              type="checkbox"
              checked={debugOn}
              onChange={(e) => setDebugOn(e.target.checked)}
            />
            Debug
          </label>

          <div ref={menuRef} style={{ position: "relative" }}>
            <button
              onClick={() => debugOn && setMenuOpen((v) => !v)}
              disabled={!debugOn}
              style={{
                padding: "8px 10px",
                borderRadius: 12,
                border: "1px solid rgba(255,255,255,0.14)",
                background: "rgba(0,0,0,0.25)",
                color: "rgba(255,255,255,0.92)",
                fontWeight: 800,
                fontSize: 12,
                opacity: debugOn ? 1 : 0.5,
                cursor: debugOn ? "pointer" : "not-allowed",
                minWidth: 170,
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                gap: 10,
              }}
            >
              {scenarioLabel(scenario)}
              <span style={{ opacity: 0.7 }}>{menuOpen ? "▴" : "▾"}</span>
            </button>

            {debugOn && menuOpen && (
              <div
                style={{
                  position: "absolute",
                  top: "calc(100% + 8px)",
                  right: 0,
                  width: 230,
                  borderRadius: 14,
                  border: "1px solid rgba(255,255,255,0.14)",
                  background: "rgba(10,16,30,0.95)",
                  backdropFilter: "blur(10px)",
                  padding: 6,
                  boxShadow: "0 18px 40px rgba(0,0,0,0.45)",
                  zIndex: 50,
                }}
              >
                <MenuItem
                  scenarioKey="normal"
                  scenario={scenario}
                  setScenario={setScenario}
                  close={() => setMenuOpen(false)}
                />
                <MenuItem
                  scenarioKey="pending50"
                  scenario={scenario}
                  setScenario={setScenario}
                  close={() => setMenuOpen(false)}
                />
                <MenuItem
                  scenarioKey="pending200"
                  scenario={scenario}
                  setScenario={setScenario}
                  close={() => setMenuOpen(false)}
                />
                <MenuItem
                  scenarioKey="avg500"
                  scenario={scenario}
                  setScenario={setScenario}
                  close={() => setMenuOpen(false)}
                />
                <MenuItem
                  scenarioKey="avg2000"
                  scenario={scenario}
                  setScenario={setScenario}
                  close={() => setMenuOpen(false)}
                />
                <MenuItem
                  scenarioKey="meltdown"
                  scenario={scenario}
                  setScenario={setScenario}
                  close={() => setMenuOpen(false)}
                />
              </div>
            )}
          </div>
        </div>
      </header>

      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}
