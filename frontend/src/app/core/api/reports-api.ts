import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from './api-config';
import { ApiPage } from './api.types';
import { ContainerHistoryEventDto } from './reports.dto';
@Injectable({ providedIn: 'root' })
export class ReportsApi {
  private readonly http = inject(HttpClient);
  history(number: string, pageIndex = 1, pageSize = 8) {
    return this.http.get<ApiPage<ContainerHistoryEventDto>>(`${API_BASE_URL}/reports/containers/${encodeURIComponent(number)}/history`, { params: { pageIndex, pageSize } });
  }
}
