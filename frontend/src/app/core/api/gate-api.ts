import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { ApiPage, ApiParams, queryParams } from './api.types';
import {
  GateTransactionDto,
  GateVehicleInput,
  UpdateGateRequest,
  ValidateGateInput,
  ValidateGateResponseDto,
} from './gate.dto';
import { QueryInvalidation } from './query-invalidation';
@Injectable({ providedIn: 'root' })
export class GateApi {
  private readonly http = inject(HttpClient);
  private readonly invalidation = inject(QueryInvalidation);
  private readonly url = API_BASE_URL + '/gate';
  validate(input: ValidateGateInput) {
    return this.http.post<ValidateGateResponseDto>(this.url + '/validate', input);
  }
  gateIn(input: ValidateGateInput & GateVehicleInput) {
    return this.http.post<{ id: string }>(this.url + '/in', input).pipe(this.changed());
  }
  gateOut(input: ValidateGateInput & GateVehicleInput) {
    return this.http.post<void>(this.url + '/out', input).pipe(this.changed());
  }
  transactions(query: ApiParams) {
    return this.http.get<ApiPage<GateTransactionDto>>(this.url + '/transactions', {
      params: queryParams(query),
    });
  }
  update(id: string, input: UpdateGateRequest) {
    return this.http.put<void>(this.url + '/transactions/' + id, input).pipe(this.changed());
  }
  cancel(id: string, reason: string) {
    return this.http
      .post<void>(this.url + '/transactions/' + id + '/cancel', { reason })
      .pipe(this.changed());
  }
  private changed<T>() {
    return tap<T>(() =>
      this.invalidation.invalidate(['gate', 'containers', 'orders', 'yard', 'reports']),
    );
  }
}
