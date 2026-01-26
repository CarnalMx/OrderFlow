import { useEffect, useState } from "react";
import type { OrderDetailDto, OrderDto } from "../api/dtos";
import {
  addOrderItem,
  cancelOrder,
  confirmOrder,
  getOrderById,
} from "../api/ordersApi";
import { formatOrderStatus, formatUtcDate } from "../utils/format";
import { Badge } from "./Badge";

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

  const buttonStyle: React.CSSProperties = {
    padding: "10px 12px",
    borderRadius: 12,
    border: "1px solid rgba(255,255,255,0.14)",
    background: "rgba(255,255,255,0.06)",
    color: "rgba(255,255,255,0.92)",
    cursor: "pointer",
    fontWeight: 800,
  };

  const dangerButtonStyle: React.CSSProperties = {
    ...buttonStyle,
    border: "1px solid rgba(255, 80, 80, 0.40)",
    background: "rgba(255, 80, 80, 0.10)",
  };

  const inputStyle: React.CSSProperties = {
    border: "1px solid rgba(255,255,255,0.14)",
    background: "rgba(0,0,0,0.22)",
    color: "rgba(255,255,255,0.92)",
    borderRadius: 12,
    padding: "10px 12px",
    outline: "none",
  };

  const cardStyle: React.CSSProperties = {
    border: "1px solid rgba(255,255,255,0.14)",
    background: "rgba(255,255,255,0.05)",
    borderRadius: 18,
    padding: 16,
  };

  const tableStyle: React.CSSProperties = {
    width: "100%",
    borderCollapse: "separate",
    borderSpacing: 0,
    overflow: "hidden",
    borderRadius: 16,
    border: "1px solid rgba(255,255,255,0.14)",
    background: "rgba(255,255,255,0.04)",
  };

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
      const updated = await confirmOrder(orderId);
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
      const updated = await cancelOrder(orderId);
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
        {error && <p style={{ color: "#ff6b6b" }}>{error}</p>}
        <p>No order loaded.</p>
      </div>
    );

  const items = order.items ?? [];

  // 0 Draft, 1 Confirmed, 2 Cancelled
  const canEdit = order.status === 0;
  const canConfirm = order.status === 0;
  const canCancel = order.status !== 2;

  return (
    <div style={{ padding: 16 }}>
      {/* Header */}
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
          gap: 12,
        }}
      >
        <div>
          <h2 style={{ marginTop: 0, marginBottom: 6 }}>Order #{order.id}</h2>
          <div style={{ opacity: 0.8 }}>
            <Badge text={formatOrderStatus(order.status)} />
          </div>
        </div>

        <div style={{ display: "flex", gap: 8 }}>
          <button onClick={handleConfirm} disabled={!canConfirm || busy} style={buttonStyle}>
            Confirm
          </button>
          <button onClick={handleCancel} disabled={!canCancel || busy} style={dangerButtonStyle}>
            Cancel
          </button>
        </div>
      </div>

      {/* Info card */}
      <div style={{ marginTop: 14, ...cardStyle }}>
        <div style={{ display: "grid", gap: 10 }}>
          <div style={{ display: "flex", justifyContent: "space-between", gap: 12 }}>
            <div style={{ opacity: 0.75 }}>Customer</div>
            <div style={{ fontWeight: 900 }}>{order.customerName}</div>
          </div>

          <div style={{ display: "flex", justifyContent: "space-between", gap: 12 }}>
            <div style={{ opacity: 0.75 }}>Status</div>
            <div>
              <Badge text={formatOrderStatus(order.status)} />
            </div>
          </div>

          <div style={{ display: "flex", justifyContent: "space-between", gap: 12 }}>
            <div style={{ opacity: 0.75 }}>Created</div>
            <div style={{ fontWeight: 800 }}>{formatUtcDate(order.createdAtUtc)}</div>
          </div>
        </div>
      </div>

      {error && (
        <div
          style={{
            marginTop: 12,
            padding: 12,
            borderRadius: 14,
            border: "1px solid rgba(255, 80, 80, 0.35)",
            background: "rgba(255, 80, 80, 0.08)",
            color: "rgba(255,255,255,0.92)",
          }}
        >
          {error}
        </div>
      )}

      {/* Items */}
      <h3 style={{ marginTop: 20, marginBottom: 10 }}>Items</h3>

      {items.length === 0 ? (
        <div style={{ opacity: 0.75 }}>No items.</div>
      ) : (
        <table style={tableStyle}>
          <thead>
            <tr style={{ background: "rgba(0,0,0,0.25)" }}>
              <th
                style={{
                  textAlign: "left",
                  padding: 12,
                  fontSize: 12,
                  opacity: 0.75,
                  borderBottom: "1px solid rgba(255,255,255,0.10)",
                }}
              >
                Name
              </th>
              <th
                style={{
                  textAlign: "right",
                  padding: 12,
                  fontSize: 12,
                  opacity: 0.75,
                  borderBottom: "1px solid rgba(255,255,255,0.10)",
                }}
              >
                Qty
              </th>
              <th
                style={{
                  textAlign: "right",
                  padding: 12,
                  fontSize: 12,
                  opacity: 0.75,
                  borderBottom: "1px solid rgba(255,255,255,0.10)",
                }}
              >
                Unit Price
              </th>
              <th
                style={{
                  textAlign: "right",
                  padding: 12,
                  fontSize: 12,
                  opacity: 0.75,
                  borderBottom: "1px solid rgba(255,255,255,0.10)",
                }}
              >
                Total
              </th>
            </tr>
          </thead>

          <tbody>
            {items.map((it) => (
              <tr key={it.id}>
                <td style={{ padding: 12, borderBottom: "1px solid rgba(255,255,255,0.08)" }}>
                  {it.name}
                </td>

                <td
                  style={{
                    padding: 12,
                    textAlign: "right",
                    borderBottom: "1px solid rgba(255,255,255,0.08)",
                    fontWeight: 700,
                  }}
                >
                  {it.quantity}
                </td>

                <td
                  style={{
                    padding: 12,
                    textAlign: "right",
                    borderBottom: "1px solid rgba(255,255,255,0.08)",
                  }}
                >
                  {it.unitPrice.toFixed(2)}
                </td>

                <td
                  style={{
                    padding: 12,
                    textAlign: "right",
                    borderBottom: "1px solid rgba(255,255,255,0.08)",
                    fontWeight: 900,
                  }}
                >
                  {(it.quantity * it.unitPrice).toFixed(2)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {/* Add item */}
      <h3 style={{ marginTop: 20, marginBottom: 10 }}>Add item</h3>

      {!canEdit && (
        <div
          style={{
            padding: 12,
            borderRadius: 14,
            border: "1px solid rgba(255,255,255,0.14)",
            background: "rgba(255,255,255,0.04)",
            opacity: 0.8,
          }}
        >
          This order is not Draft, items can no longer be added.
        </div>
      )}

      <div
        style={{
          marginTop: 10,
          display: "flex",
          gap: 10,
          alignItems: "center",
          flexWrap: "wrap",
        }}
      >
        <input
          placeholder="Item name"
          value={name}
          disabled={!canEdit || busy}
          onChange={(e) => setName(e.target.value)}
          style={{ ...inputStyle, minWidth: 220 }}
        />

        <input
          type="number"
          value={quantity}
          disabled={!canEdit || busy}
          onChange={(e) => setQuantity(Number(e.target.value))}
          style={{ ...inputStyle, width: 90 }}
        />

        <input
          type="number"
          value={unitPrice}
          disabled={!canEdit || busy}
          onChange={(e) => setUnitPrice(Number(e.target.value))}
          style={{ ...inputStyle, width: 140 }}
        />

        <button
          onClick={handleAddItem}
          disabled={!canEdit || busy || name.trim().length === 0}
          style={buttonStyle}
        >
          Add
        </button>
      </div>
    </div>
  );
}
