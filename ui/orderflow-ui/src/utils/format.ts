import type { OrderStatus } from "../api/dtos";

export function formatOrderStatus(status: OrderStatus): string {
  switch (status) {
    case 0:
      return "Draft";
    case 1:
      return "Confirmed";
    case 2:
      return "Cancelled";
    default:
      return "Unknown";
  }
}

export function formatUtcDate(isoUtc: string): string {
  const d = new Date(isoUtc);

  // Si el string viene mal o null
  if (Number.isNaN(d.getTime())) return isoUtc;

  return d.toLocaleString();
}
