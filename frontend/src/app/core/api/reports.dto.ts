export interface MovementPositionDto { blockId: string; blockCode: string; bay: number; row: number; tier: number; }
export interface ContainerHistoryEventDto {
  eventId: string; source: string; eventType: string; lifecycleId: string; occurredAtUtc: string;
  fromPosition: MovementPositionDto | null; toPosition: MovementPositionDto | null;
  actorUserId: string | null; vehicleNumber: string | null; vehicleType: string | null; deliveryOrderId: string | null;
}
