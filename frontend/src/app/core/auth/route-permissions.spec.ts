import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of } from 'rxjs';
import { describe, expect, it } from 'vitest';
import { Component, signal } from '@angular/core';
import { authGuard } from './auth.guard';
import { AuthService } from './auth-service';

@Component({ template: 'page' })
class Page {}
describe('route permissions', () => {
  it('blocks direct gate access for OrderClerk before loading the page', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: 'gate',
            canActivate: [authGuard],
            data: { capability: 'manageGate' },
            component: Page,
          },
          { path: 'dashboard', component: Page },
        ]),
        {
          provide: AuthService,
          useValue: {
            restore: () => of(undefined),
            user: signal({ roles: ['OrderClerk'] }),
            status: signal('authenticated'),
          },
        },
      ],
    });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/gate');
    expect(TestBed.inject(Router).url).toBe('/dashboard');
    TestBed.resetTestingModule();
  });
});
