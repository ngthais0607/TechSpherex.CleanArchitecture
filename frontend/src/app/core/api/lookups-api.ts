import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from './api-config';
import { LookupsDto } from './lookups.dto';
@Injectable({ providedIn: 'root' })
export class LookupsApi {
  private readonly http = inject(HttpClient);
  get() { return this.http.get<LookupsDto>(API_BASE_URL + '/lookups'); }
}
