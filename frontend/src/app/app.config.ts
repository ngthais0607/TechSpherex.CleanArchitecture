import { ApplicationConfig, provideBrowserGlobalErrorListeners, LOCALE_ID } from '@angular/core';
import { registerLocaleData, DATE_PIPE_DEFAULT_OPTIONS } from '@angular/common';
import vi from '@angular/common/locales/vi';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
registerLocaleData(vi);

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: LOCALE_ID, useValue: 'vi-VN' },
    { provide: DATE_PIPE_DEFAULT_OPTIONS, useValue: { timezone: '+0700' } },
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
  ],
};
