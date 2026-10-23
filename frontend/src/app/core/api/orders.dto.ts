import { ApiPage } from './api.types';
import { ContainerTypeCode } from './containers.dto';
export interface DeliveryOrderItemDto {
  id: string;
  size: 'Ft20' | 'Ft40';
  type: ContainerTypeCode;
  requiredQuantity: number;
  deliveredQuantity: number;
  remainingQuantity: number;
}
export interface DeliveryOrderDto {
  id: string;
  orderNumber: string;
  lineOperator: string;
  customerTaxCode: string;
  customerName: string;
  expiryDate: string;
  outboundVessel: string;
  isCompleted: boolean;
  items: DeliveryOrderItemDto[];
  status: string;
}
export interface CreateOrderRequest {
  orderNumber: string;
  lineOperator: string;
  customerTaxCode: string;
  customerName: string;
  expiryDate: string;
  outboundVessel: string;
  items: { size: 'Ft20' | 'Ft40'; type: ContainerTypeCode; quantity: number }[];
}
export interface OrderBookingDto {
  id: string;
  itemId: string;
  containerNumber: string;
  status: string;
  reservedAt: string;
  shippedAt: string | null;
}
export interface OrderBookingsDto {
  bookings: ApiPage<OrderBookingDto>;
  itemCounts: {
    itemId: string;
    reservedQuantity: number;
    shippedQuantity: number;
    availableQuantity: number;
  }[];
}
export interface AssignContainersResult {
  orderId: string;
  requestedCount: number;
  assignedCount: number;
  bookingIds: string[];
  failedContainerNumbers: string[];
}
