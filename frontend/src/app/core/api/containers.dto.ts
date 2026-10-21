export type ContainerTypeCode = 'Dry' | 'Reefer' | 'OpenTop' | 'FlatRack' | 'Bunker' | 'Ventilated' | 'Specialized';
export type ContainerConditionCode = 'Normal' | 'Damaged' | 'Dented' | 'Bent' | 'Warped';
export interface SlotDto { blockCode: string; bay: number; row: number; tier: number; }
export interface ContainerDto {
  id: string; containerNumber: string; isoCode: string; size: 'Ft20' | 'Ft40'; type: ContainerTypeCode;
  tareWeight: number; maxGrossWeight: number; ownerCode: string; condition: ContainerConditionCode;
  classification: 'A' | 'B' | 'C'; dateOfManufacture: string | null; displayStatus: string;
  currentSlot: SlotDto | null; currentBooking: { orderNumber: string; orderExpiry: string } | null; isInYard: boolean;
}
export interface ContainerSummaryDto { total: number; bySize: Record<string, number>; byType: Record<string, number>; byCondition: Record<string, number>; }
export interface OrderSummaryDto { orderId: string; orderNumber: string; lineOperator: string; customerName: string; expiryDate: string; status: string; }
export interface ContainerOrdersDto {
  currentOrder: OrderSummaryDto | null; summary: { customerCount: number; shipmentCount: number; lastCustomer: string | null };
  history: (Omit<OrderSummaryDto, 'orderId'> & {orderId: string; shippedDate: string | null})[];
}
export interface RegisterContainerRequest {
  containerNumber: string; isoCode: string; size: 'Ft20' | 'Ft40'; type: ContainerTypeCode;
  tareWeight: number; maxGrossWeight: number; dateOfManufacture: string | null; grade: string; condition: ContainerConditionCode;
}
