export type OrderStatus = 0 | 1 | 2;

export interface OrderDto {
  id: number;
  customerName: string;
  createdAtUtc: string;
  status: OrderStatus;
}

export interface CreateOrderRequest {
  customerName: string;
}

export interface OrderItemDto {
  id: number;
  orderId: number;
  name: string;
  quantity: number;
  unitPrice: number;
}

export interface OrderDetailDto extends OrderDto {
  items?: OrderItemDto[];
}

export interface AddOrderItemRequest {
  name: string;
  quantity: number;
  unitPrice: number;
}
