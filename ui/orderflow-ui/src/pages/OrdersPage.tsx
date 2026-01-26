import { useEffect, useState } from "react";
import { SplitView } from "../components/SplitView";
import { createOrder, getOrders } from "../api/ordersApi";
import type { OrderDto } from "../api/dtos";
import { formatOrderStatus } from "../utils/format";
import { OrderDetailPanel } from "../components/OrderDetailPanel";
import { WorkerBox } from "../components/WorkerBox";
import { Badge } from "../components/Badge";

export function OrdersPage() {
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function refresh() {
    setLoading(true);
    setError(null);

    try {
      const data = await getOrders();
      setOrders(data);

      // Si no hay seleccion, selecciona el primero
      if (data.length > 0 && selectedOrderId === null) {
        setSelectedOrderId(data[0].id);
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
  }, []);

  async function handleCreate() {
    const customerName = prompt("Customer name?");
    if (!customerName) return;

    await createOrder({ customerName });
    await refresh();
  }

  function updateOrderInList(updated: OrderDto) {
    setOrders((prev) =>
      prev.map((o) => (o.id === updated.id ? { ...o, status: updated.status } : o))
    );
  }

  const buttonStyle: React.CSSProperties = {
    padding: "10px 12px",
    borderRadius: 12,
    border: "1px solid rgba(255,255,255,0.14)",
    background: "rgba(255,255,255,0.06)",
    color: "rgba(255,255,255,0.92)",
    cursor: "pointer",
    fontWeight: 800,
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

  return (
    <SplitView
      left={
        <div style={{ height: "100%", display: "flex", flexDirection: "column" }}>
          {/* TOP (scroll) */}
          <div style={{ flex: 1, overflow: "auto", padding: 16 }}>
            <div
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                marginBottom: 12,
              }}
            >
              <h2 style={{ margin: 0 }}>Orders</h2>

              <button onClick={handleCreate} style={buttonStyle}>
                Create
              </button>
            </div>

            {loading && <p style={{ opacity: 0.8 }}>Loading...</p>}

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
              {orders.map((o) => (
                <li key={o.id}>
                  <button
                    onClick={() => setSelectedOrderId(o.id)}
                    style={listItemStyle(o.id === selectedOrderId)}
                  >
                    <div style={{ fontWeight: 900 }}>
                      #{o.id} - {o.customerName}
                    </div>

                    <div style={{ marginTop: 6 }}>
                      <Badge text={formatOrderStatus(o.status)} />
                    </div>
                  </button>
                </li>
              ))}
            </ul>

            {!loading && orders.length === 0 && (
              <div
                style={{
                  marginTop: 12,
                  padding: 12,
                  borderRadius: 14,
                  border: "1px solid rgba(255,255,255,0.14)",
                  background: "rgba(255,255,255,0.04)",
                  color: "rgba(255,255,255,0.75)",
                }}
              >
                No orders yet. Create one.
              </div>
            )}
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
        selectedOrderId ? (
          <div style={{ padding: 16 }}>
            <OrderDetailPanel orderId={selectedOrderId} onOrderUpdated={updateOrderInList} />
          </div>
        ) : (
          <div style={{ padding: 16, opacity: 0.8 }}>Select an order...</div>
        )
      }
    />
  );
}
