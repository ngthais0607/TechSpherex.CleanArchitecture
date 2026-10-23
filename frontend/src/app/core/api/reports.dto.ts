export interface MovementPositionDto {
  blockId: string;
  blockCode: string;
  bay: number;
  row: number;
  tier: number;
}
export interface ContainerHistoryEventDto {
  eventId: string;
  source: string;
  eventType: string;
  lifecycleId: string;
  occurredAtUtc: string;
  fromPosition: MovementPositionDto | null;
  toPosition: MovementPositionDto | null;
  actorUserId: string | null;
  vehicleNumber: string | null;
  vehicleType: string | null;
  deliveryOrderId: string | null;
}
export interface DashboardSummaryDto {
  totalContainersInYard: number;
  totalTeuInYard: number;
  activeOrdersCount: number;
  todayGateTransactionsCount: number;
}
export interface DailyGateTrafficDto {
  date: string;
  gateInCount: number;
  gateOutCount: number;
  totalCount: number;
}
export interface BlockCapacityDto {
  blockId: string;
  blockCode: string;
  blockName: string;
  totalCapacityTeu: number;
  occupiedTeu: number;
  occupancyRate: number;
  containersCount: number;
}
export interface InventoryGroupDto {
  lineOperator: string;
  size: string;
  type: string;
  condition: string;
  blockId: string | null;
  blockCode: string | null;
  containerCount: number;
}
export interface InventoryReportDto {
  groups: import('./api.types').ApiPage<InventoryGroupDto>;
  totalContainers: number;
}
export interface DwellTimeDto {
  lifecycleId: string;
  containerId: string;
  containerNumber: string;
  lineOperator: string;
  inDate: string;
  outDate: string | null;
  daysInDepot: number;
}
export interface ExpiringDeliveryOrderDto {
  id: string;
  orderNumber: string;
  lineOperator: string;
  customerName: string;
  expiryDate: string;
  daysUntilExpiry: number;
  remainingQuantity: number;
}
