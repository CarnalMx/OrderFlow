import { API_BASE_URL } from "./http";
import type { CreateOrderRequest, OrderDto } from "./dtos";
import type { AddOrderItemRequest, OrderDetailDto } from "./dtos";

export async function getOrders(): Promise<OrderDto[]> {
  const res = await fetch(`${API_BASE_URL}/orders`);
  if (!res.ok) throw new Error("Failed to fetch orders");
  return res.json();
}

export async function createOrder(request: CreateOrderRequest): Promise<OrderDto> {
  const res = await fetch(`${API_BASE_URL}/orders`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!res.ok) throw new Error("Failed to create order");
  return res.json();
}

export async function getOrderById(orderId: number): Promise<OrderDetailDto> {
  const res = await fetch(`${API_BASE_URL}/orders/${orderId}`);
  if (!res.ok) throw new Error("Failed to fetch order");
  return res.json();
}

export async function addOrderItem(orderId: number, request: AddOrderItemRequest) {
  const res = await fetch(`${API_BASE_URL}/orders/${orderId}/items`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!res.ok) throw new Error("Failed to add item");
  return res.json();
}

export async function confirmOrder(orderId: number) {
  const res = await fetch(`${API_BASE_URL}/orders/${orderId}/confirm`, {
    method: "POST",
  });

  if (!res.ok) throw new Error("Failed to confirm order");
  return res.json();
}

export async function cancelOrder(orderId: number) {
  const res = await fetch(`${API_BASE_URL}/orders/${orderId}/cancel`, {
    method: "POST",
  });

  if (!res.ok) throw new Error("Failed to cancel order");
  return res.json();
}
