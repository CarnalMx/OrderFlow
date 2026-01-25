import { useEffect, useState } from "react";
import { SplitView } from "../components/SplitView";
import { createOrder, getOrders } from "../api/ordersApi";
import type { OrderDto } from "../api/dtos";
import { formatOrderStatus } from "../utils/format";
import { OrderDetailPanel } from "../components/OrderDetailPanel";

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


  return (
    <SplitView
      left={
        <div style={{ padding: 16 }}>
          <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 12 }}>
            <h2 style={{ margin: 0 }}>Orders</h2>
            <button onClick={handleCreate}>Create</button>
          </div>

          {loading && <p>Loading...</p>}
          {error && <p style={{ color: "red" }}>{error}</p>}

          <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
            {orders.map((o) => (
              <li key={o.id}>
                <button
                  onClick={() => setSelectedOrderId(o.id)}
                  style={{
                    width: "100%",
                    textAlign: "left",
                    padding: 12,
                    marginBottom: 8,
                    border: "1px solid #ddd",
                    background: o.id === selectedOrderId ? "#f5f5f5" : "white",
                    cursor: "pointer",
                  }}
                >
                  <div style={{ fontWeight: 600 }}>#{o.id} - {o.customerName}</div>
                  <div style={{ fontSize: 12, opacity: 0.7 }}>{formatOrderStatus(o.status)}</div>
                </button>
              </li>
            ))}
          </ul>
        </div>
      }
      right={
        selectedOrderId ? (
          <OrderDetailPanel
            orderId={selectedOrderId}
            onOrderUpdated={updateOrderInList}
            />
        ) : (
        <div style={{ padding: 16 }}>Select an order...</div>
        )
      }
    />
  );
}
