import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { ApiPage, ApiParams, queryParams } from './api.types';
import { CustomerDto, LineOperatorDto, OwnerCodeDto } from './partners.dto';
import { QueryInvalidation } from './query-invalidation';
@Injectable({ providedIn: 'root' })
export class PartnersApi {
  private readonly http = inject(HttpClient);
  private readonly invalidation = inject(QueryInvalidation);
  customers(params: ApiParams = {}) { return this.http.get<ApiPage<CustomerDto>>(API_BASE_URL + '/customers', { params: queryParams(params) }); }
  operators(params: ApiParams = {}) { return this.http.get<ApiPage<LineOperatorDto>>(API_BASE_URL + '/line-operators', { params: queryParams(params) }); }
  ownerCodes(operatorId: string) { return this.http.get<OwnerCodeDto[]>(`${API_BASE_URL}/line-operators/${encodeURIComponent(operatorId)}/owner-codes`); }
  createCustomer(input: { taxCode: string; name: string }) { return this.http.post<{id: string}>(API_BASE_URL + '/customers', input).pipe(this.changed()); }
  createOperator(input: { code: string; name: string }) { return this.http.post<{id: string}>(API_BASE_URL + '/line-operators', input).pipe(this.changed()); }
  update(kind: 'customers' | 'line-operators', id: string, name: string) { return this.http.put<void>(`${API_BASE_URL}/${kind}/${id}`, { name }).pipe(this.changed()); }
  setActive(kind: 'customers' | 'line-operators', id: string, active: boolean) { return this.http.post<void>(`${API_BASE_URL}/${kind}/${id}/${active ? 'activate' : 'deactivate'}`, {}).pipe(this.changed()); }
  addOwnerCode(id: string, ownerCode: string) { return this.http.post<void>(`${API_BASE_URL}/line-operators/${id}/owner-codes`, { ownerCode }).pipe(this.changed()); }
  removeOwnerCode(id: string, ownerCode: string) { return this.http.delete<void>(`${API_BASE_URL}/line-operators/${id}/owner-codes/${encodeURIComponent(ownerCode)}`).pipe(this.changed()); }
  private changed<T>() { return tap<T>(() => this.invalidation.invalidate(['partners', 'lookups', 'orders', 'containers', 'reports'])); }
}
