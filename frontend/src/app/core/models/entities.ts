export type ContainerType =
  'Dry' | 'Reefer' | 'Open Top' | 'Flat Rack' | 'Bunker' | 'Ventilated' | 'Specialized';
export const TYPES: ContainerType[] = [
  'Dry',
  'Reefer',
  'Open Top',
  'Flat Rack',
  'Bunker',
  'Ventilated',
  'Specialized',
];
export const CONDITIONS = ['Bình thường', 'Móp', 'Méo', 'Cong', 'Hư hỏng khác'];
export type ContainerSize = 20 | 40;
export type Grade = 'A' | 'B' | 'C';
export type Position =
  | { kind: 'physical'; blockId: string; bay: number; row: string; tier: number }
  | { kind: 'virtual'; blockId: string };
export interface Container {
  number: string;
  type: ContainerType;
  size: ContainerSize;
  iso: string;
  ownerCode: string;
  operatorId: string;
  grade: Grade;
  condition: string;
  maxWeight: number;
  tareWeight: number;
  manufactureMonth: string;
}
export interface Customer {
  id: string;
  taxCode: string;
  name: string;
  email: string;
  phone: string;
  address: string;
  isActive: boolean;
}
export interface Operator {
  id: string;
  code: string;
  name: string;
  contact: string;
  isActive: boolean;
  color: string;
}
export interface OrderLine {
  id: string;
  type: ContainerType;
  size: ContainerSize;
  quantity: number;
}
export interface DeliveryOrder {
  id: string;
  number: string;
  customerId: string;
  operatorId: string;
  expiry: string;
  voyage: string;
  lines: OrderLine[];
}
export interface Booking {
  id: string;
  number: string;
  orderId: string;
  lineId: string;
  lifecycleId: string;
  status: 'Reserved' | 'Shipped' | 'Cancelled' | 'Expired';
  createdAt: string;
  shippedAt?: string;
}
export interface Lifecycle {
  id: string;
  number: string;
  inAt: string;
  outAt?: string;
  cancelled?: boolean;
  position?: Position;
}
export interface Block {
  id: string;
  name: string;
  oddBays: number;
  rows: number;
  tiers: number;
  virtual: boolean;
}
export interface GateInput {
  number: string;
  orderId?: string;
  vehicleType: 'Truck' | 'Barge';
  vehicleNo: string;
  driver: string;
  grade: Grade;
  condition: string;
  target?: Position;
}
export interface GateTransaction extends GateInput {
  id: string;
  direction: 'In' | 'Out';
  at: string;
  status: 'Active' | 'Cancelled';
  lifecycleId: string;
  createdBy: string;
  previousPosition?: Position;
  previousGrade: Grade;
  previousCondition: string;
  changes: { at: string; user: string; field: string; oldValue: string; newValue: string }[];
}
export interface Movement {
  id: string;
  number: string;
  at: string;
  from?: Position;
  to: Position;
}
export interface DepotSnapshot {
  schemaVersion: 1;
  revision: number;
  seededAt: string;
  containers: Container[];
  customers: Customer[];
  operators: Operator[];
  orders: DeliveryOrder[];
  bookings: Booking[];
  lifecycles: Lifecycle[];
  blocks: Block[];
  gateTransactions: GateTransaction[];
  movements: Movement[];
}
export type Result<T = void> =
  | { ok: true; value: T }
  | { ok: false; error: { code: string; message: string; fields?: Record<string, string> } };
export interface OrderInput {
  number: string;
  customerId: string;
  operatorId: string;
  expiry: string;
  voyage: string;
  lines: Omit<OrderLine, 'id'>[];
}
export interface GateCheck {
  code: string;
  label: string;
  state: 'wait' | 'pass' | 'error';
  detail: string;
}
export interface Filters {
  search?: string;
  type?: string;
  size?: string;
  operatorId?: string;
  condition?: string;
  booking?: string;
  status?: string;
  customerId?: string;
  page?: number;
  pageSize?: number;
  sort?: string;
  from?: string;
  to?: string;
}
