import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { ApiPage, ApiParams, queryParams } from './api.types';
import {
  ContainerDto,
  ContainerOrdersDto,
  ContainerSummaryDto,
  RegisterContainerRequest,
} from './containers.dto';
import { QueryInvalidation } from './query-invalidation';
@Injectable({ providedIn: 'root' })
export class ContainersApi {
  private readonly http = inject(HttpClient);
  private readonly invalidation = inject(QueryInvalidation);
  list(query: ApiParams) {
    return this.http.get<ApiPage<ContainerDto>>(API_BASE_URL + '/containers', {
      params: queryParams(query),
    });
  }
  summary(query: ApiParams) {
    const { pageIndex, pageSize, sort, descending, ...filters } = query;
    return this.http.get<ContainerSummaryDto>(API_BASE_URL + '/containers/summary', {
      params: queryParams(filters),
    });
  }
  byNumber(number: string) {
    return this.http.get<ContainerDto>(
      `${API_BASE_URL}/containers/by-number/${encodeURIComponent(number)}`,
    );
  }
  orders(number: string) {
    return this.http.get<ContainerOrdersDto>(
      `${API_BASE_URL}/containers/${encodeURIComponent(number)}/orders`,
    );
  }
  register(input: RegisterContainerRequest) {
    return this.http
      .post<{ id: string }>(API_BASE_URL + '/containers', input)
      .pipe(tap(() => this.invalidation.invalidate(['containers', 'reports'])));
  }
  damage(id: string, condition: string) {
    return this.http
      .post<void>(`${API_BASE_URL}/containers/${id}/damage`, { condition })
      .pipe(tap(() => this.invalidation.invalidate(['containers', 'yard', 'reports'])));
  }
}
