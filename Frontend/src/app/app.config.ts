import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeEsEc from '@angular/common/locales/es-EC';
import {
  ApplicationConfig,
  DEFAULT_CURRENCY_CODE,
  LOCALE_ID,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { interceptorErrores } from './nucleo/interceptores/interceptor-errores';
import { interceptorToken } from './nucleo/interceptores/interceptor-token';

registerLocaleData(localeEsEc);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: LOCALE_ID, useValue: 'es-EC' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'USD' },
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([interceptorErrores, interceptorToken])),
  ],
};
