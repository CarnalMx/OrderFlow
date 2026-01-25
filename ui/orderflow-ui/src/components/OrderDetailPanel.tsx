import { useEffect, useState } from "react";
import type { OrderDetailDto, OrderDto } from "../api/dtos";
import { addOrderItem, cancelOrder, confirmOrder, getOrderById } from "../api/ordersApi";
import { formatOrderStatus, formatUtcDate } from "../utils/format";

export function OrderDetailPanel(props: {
  orderId: number;
  onOrderUpdated?: (updated: OrderDto) => void;
}) {
  const { orderId, onOrderUpdated } = props;

  const [order, setOrder] = useState<OrderDetailDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // form state
  const [name, setName] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [unitPrice, setUnitPrice] = useState(1);

  async function load() {
    setLoading(true);
    setError(null);

    try {
      const data = await getOrderById(orderId);
      setOrder(data);
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orderId]);

  async function handleAddItem() {
    if (!order) return;

    setBusy(true);
    setError(null);

    try {
      await addOrderItem(orderId, {
        name,
        quantity,
        unitPrice,
      });

      setName("");
      setQuantity(1);
      setUnitPrice(1);

      await load();
    } catch (e) {
      setError(String(e));
    } finally {
      setBusy(false);
    }
  }

  async function handleConfirm() {
  setBusy(true);
  setError(null);

  try {
    const updated = await confirmOrder(orderId); // 👈 aqui esta el updated
    await load();
    onOrderUpdated?.(updated);
  } catch (e) {
    setError(String(e));
  } finally {
    setBusy(false);
  }
}

  async function handleCancel() {
  setBusy(true);
  setError(null);

  try {
    const updated = await cancelOrder(orderId); // 👈 aqui esta el updated
    await load();
    onOrderUpdated?.(updated);
  } catch (e) {
    setError(String(e));
  } finally {
    setBusy(false);
  }
}

  if (loading) return <div style={{ padding: 16 }}>Loading order...</div>;

  if (!order)
    return (
      <div style={{ padding: 16 }}>
        <h2 style={{ marginTop: 0 }}>Order Detail</h2>
        {error && <p style={{ color: "red" }}>{error}</p>}
        <p>No order loaded.</p>
      </div>
    );

  const items = order.items ?? [];

  const canEdit = order.status === 0; // Draft
  const canConfirm = order.status === 0;
  const canCancel = order.status !== 2; // not cancelled

  return (
    <div style={{ padding: 16 }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h2 style={{ marginTop: 0, marginBottom: 0 }}>Order #{order.id}</h2>

        <div style={{ display: "flex", gap: 8 }}>
          <button onClick={handleConfirm} disabled={!canConfirm || busy}>
            Confirm
          </button>
          <button onClick={handleCancel} disabled={!canCancel || busy}>
            Cancel
          </button>
        </div>
      </div>

      <div style={{ marginTop: 12, border: "1px solid #ddd", padding: 16 }}>
        <p style={{ marginTop: 0 }}>
          <b>Customer:</b> {order.customerName}
        </p>
        <p>
          <b>Status:</b> {formatOrderStatus(order.status)}
        </p>
        <p style={{ marginBottom: 0 }}>
          <b>Created:</b> {formatUtcDate(order.createdAtUtc)}
        </p>
      </div>

      {error && <p style={{ color: "red", marginTop: 12 }}>{error}</p>}

      <h3 style={{ marginTop: 20 }}>Items</h3>

      {items.length === 0 ? (
        <p>No items.</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr>
              <th style={{ textAlign: "left", borderBottom: "1px solid #ddd", padding: 8 }}>Name</th>
              <th style={{ textAlign: "right", borderBottom: "1px solid #ddd", padding: 8 }}>
                Qty
              </th>
              <th style={{ textAlign: "right", borderBottom: "1px solid #ddd", padding: 8 }}>
                Unit Price
              </th>
              <th style={{ textAlign: "right", borderBottom: "1px solid #ddd", padding: 8 }}>
                Total
              </th>
            </tr>
          </thead>
          <tbody>
            {items.map((it) => (
              <tr key={it.id}>
                <td style={{ padding: 8, borderBottom: "1px solid #eee" }}>{it.name}</td>
                <td style={{ padding: 8, textAlign: "right", borderBottom: "1px solid #eee" }}>
                  {it.quantity}
                </td>
                <td style={{ padding: 8, textAlign: "right", borderBottom: "1px solid #eee" }}>
                  {it.unitPrice.toFixed(2)}
                </td>
                <td style={{ padding: 8, textAlign: "right", borderBottom: "1px solid #eee" }}>
                  {(it.quantity * it.unitPrice).toFixed(2)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <h3 style={{ marginTop: 20 }}>Add item</h3>

      {!canEdit && (
        <p style={{ opacity: 0.7 }}>
          This order is not Draft, items can no longer be added.
        </p>
      )}

      <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
        <input
          placeholder="Item name"
          value={name}
          disabled={!canEdit || busy}
          onChange={(e) => setName(e.target.value)}
        />

        <input
          type="number"
          value={quantity}
          disabled={!canEdit || busy}
          onChange={(e) => setQuantity(Number(e.target.value))}
          style={{ width: 80 }}
        />

        <input
          type="number"
          value={unitPrice}
          disabled={!canEdit || busy}
          onChange={(e) => setUnitPrice(Number(e.target.value))}
          style={{ width: 120 }}
        />

        <button onClick={handleAddItem} disabled={!canEdit || busy || name.trim().length === 0}>
          Add
        </button>
      </div>
    </div>
  );
}
