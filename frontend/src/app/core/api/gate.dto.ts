export interface ValidateGateInput {
  containerNumber: string;
  type: 'GateIn' | 'GateOut';
  orderNumber?: string;
  blockCode?: string;
  bay?: number;
  row?: number;
  tier?: number;
  virtualBlockCode?: string;
}
export interface RuleCheckDto {
  ruleCode: string;
  state: 'Pass' | 'Fail' | 'Warning';
  label: string;
  detail: string | null;
}
export interface ValidateGateResponseDto {
  checks: RuleCheckDto[];
  container: {
    id: string;
    containerNumber: string;
    isoCode: string;
    size: string;
    type: string;
    ownerCode: string;
    displayStatus: string;
    currentLocation: string | null;
    currentBookingOrder: string | null;
  } | null;
  order: {
    id: string;
    orderNumber: string;
    lineOperator: string;
    customerName: string;
    expiryDate: string;
    items: {
      id: string;
      size: string;
      type: string;
      quantity: number;
      reservedQuantity: number;
      shippedQuantity: number;
      remainingQuantity: number;
    }[];
  } | null;
}
export interface GateVehicleInput {
  vehicleNumber: string;
  vehicleType: string;
  driverName?: string;
  grade?: string;
  condition?: string;
}
export interface GateTransactionDto {
  id: string;
  occurredAt: string;
  type: string;
  status: string;
  containerNumber: string;
  size: string | null;
  containerType: string | null;
  vehicleNumber: string;
  vehicleType: string;
  driverName: string | null;
  position: string | null;
  orderNumber: string | null;
  createdBy: string | null;
  cancelledAt: string | null;
  cancelledBy: string | null;
  changes: {
    id: string;
    fieldName: string;
    oldValue: string | null;
    newValue: string | null;
    changedAt: string;
    changedBy: string | null;
  }[];
}
export interface UpdateGateRequest {
  vehicleNumber?: string;
  vehicleType?: string;
  driverName?: string;
  grade?: string;
  condition?: string;
  note?: string;
}
