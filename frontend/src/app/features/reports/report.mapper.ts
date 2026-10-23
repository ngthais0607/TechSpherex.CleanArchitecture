import {
  ContainerHistoryEventDto,
  DwellTimeDto,
  ExpiringDeliveryOrderDto,
  InventoryReportDto,
} from '../../core/api/reports.dto';
export function inventoryView(dto: InventoryReportDto) {
  return { ...dto, totalContainers: dto.totalContainers, totalGroups: dto.groups.totalCount };
}
export interface ReportRow {
  id: string;
  cells: (string | number | null)[];
}
export const reportHeaders = {
  dwell: ['Container', 'Hãng', 'Ngày nhập UTC', 'Ngày xuất UTC', 'Ngày lưu bãi'],
  expiring: ['Số lệnh', 'Hãng', 'Khách hàng', 'Ngày hết hạn UTC', 'Còn ngày', 'Số lượng còn'],
  history: ['Thời điểm UTC', 'Sự kiện', 'Từ block', 'Đến block', 'Phương tiện'],
};
export function dwellRow(row: DwellTimeDto): ReportRow {
  return {
    id: row.lifecycleId,
    cells: [row.containerNumber, row.lineOperator, row.inDate, row.outDate, row.daysInDepot],
  };
}
export function expiringRow(row: ExpiringDeliveryOrderDto): ReportRow {
  return {
    id: row.id,
    cells: [
      row.orderNumber,
      row.lineOperator,
      row.customerName,
      row.expiryDate,
      row.daysUntilExpiry,
      row.remainingQuantity,
    ],
  };
}
export function historyRow(row: ContainerHistoryEventDto): ReportRow {
  return {
    id: row.eventId,
    cells: [
      row.occurredAtUtc,
      row.eventType,
      row.fromPosition?.blockCode ?? null,
      row.toPosition?.blockCode ?? null,
      row.vehicleNumber,
    ],
  };
}
