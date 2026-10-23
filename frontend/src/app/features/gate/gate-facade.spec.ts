import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { GateFacade } from './gate-facade';
import { errorInterceptor } from '../../core/api/error.interceptor';
import { mapGateChecks } from './gate.mapper';
const input = {
  containerNumber: 'MSCU9000008',
  type: 'GateIn' as const,
  virtualBlockCode: 'VB-01',
};
const result = {
  checks: [{ ruleCode: 'G1', state: 'Warning' as const, label: 'Check', detail: null }],
  container: null,
  order: null,
};
describe('gate commands', () => {
  let http: HttpTestingController;
  let facade: GateFacade;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        GateFacade,
      ],
    });
    http = TestBed.inject(HttpTestingController);
    facade = TestBed.inject(GateFacade);
  });
  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });
  it('mapsPassFailWarningAndIgnoresStaleValidation', () => {
    expect(mapGateChecks(result.checks)[0].state).toBe('Warning');
    facade.setInput(input);
    const old = http.expectOne('/api/gate/validate');
    facade.setInput({ ...input, containerNumber: 'MEDU9000030' });
    expect(old.cancelled).toBe(true);
    http.expectOne('/api/gate/validate').flush(result);
    expect(facade.validation()?.checks[0].state).toBe('Warning');
  });
  it('doubleSubmitSendsOneCommand', () => {
    facade.setInput(input);
    http.expectOne('/api/gate/validate').flush(result);
    facade
      .submit({ vehicleNumber: 'TRUCK', vehicleType: 'Truck', grade: 'A', condition: 'Normal' })
      .subscribe();
    facade
      .submit({ vehicleNumber: 'TRUCK', vehicleType: 'Truck', grade: 'A', condition: 'Normal' })
      .subscribe({ error: () => {} });
    http.expectOne('/api/gate/in').flush({ id: 'life' });
    expect(facade.pending()).toBe(false);
  });
  it('timeoutDoesNotReplayWrite', () => {
    facade.setInput(input);
    http.expectOne('/api/gate/validate').flush(result);
    facade
      .submit({ vehicleNumber: 'TRUCK', vehicleType: 'Truck', grade: 'A', condition: 'Normal' })
      .subscribe({ error: () => {} });
    http.expectOne('/api/gate/in').error(new ProgressEvent('timeout'));
    http.expectNone('/api/gate/in');
    expect(facade.input()?.containerNumber).toBe(input.containerNumber);
  });
  it('cancelConflictKeepsDetail', () => {
    facade.selected.set({ id: 'tx', status: 'Active' } as never);
    facade.cancel('tx', 'reason').subscribe({ error: () => {} });
    const request = http.expectOne('/api/gate/transactions/tx/cancel');
    expect(request.request.method).toBe('POST');
    request.flush(
      { detail: 'Occupied', code: 'G9.SlotOccupied' },
      { status: 409, statusText: 'Conflict' },
    );
    expect(facade.selected()?.id).toBe('tx');
    expect(facade.error()?.status).toBe(409);
  });
});
