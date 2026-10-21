import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from './api-config';
import { ApiPage, ApiParams, queryParams } from './api.types';
import { BlockCapacityDto, ContainerHistoryEventDto, DailyGateTrafficDto, DashboardSummaryDto, DwellTimeDto, ExpiringDeliveryOrderDto, InventoryReportDto } from './reports.dto';
@Injectable({ providedIn: 'root' })
export class ReportsApi {
  private readonly http = inject(HttpClient);
  summary(){return this.http.get<DashboardSummaryDto>(API_BASE_URL+'/reports/summary');}
  traffic(days=7){return this.http.get<DailyGateTrafficDto[]>(API_BASE_URL+'/reports/gate-traffic',{params:{days}});}
  capacity(){return this.http.get<BlockCapacityDto[]>(API_BASE_URL+'/reports/block-capacity');}
  inventory(query:ApiParams){return this.http.get<InventoryReportDto>(API_BASE_URL+'/reports/yard-inventory',{params:queryParams(query)});}
  dwellTimes(query:ApiParams){return this.http.get<ApiPage<DwellTimeDto>>(API_BASE_URL+'/reports/dwell-times',{params:queryParams(query)});}
  expiringOrders(query:ApiParams){return this.http.get<ApiPage<ExpiringDeliveryOrderDto>>(API_BASE_URL+'/reports/expiring-orders',{params:queryParams(query)});}
  history(number: string, pageIndex = 1, pageSize = 8) {
    return this.http.get<ApiPage<ContainerHistoryEventDto>>(`${API_BASE_URL}/reports/containers/${encodeURIComponent(number)}/history`, { params: { pageIndex, pageSize } });
  }
}
