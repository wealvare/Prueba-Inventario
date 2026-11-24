import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import localeEsEc from '@angular/common/locales/es-EC';
import {
  ApplicationConfig,
  DEFAULT_CURRENCY_CODE,
  ErrorHandler,
  LOCALE_ID,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import {
  provideRouter,
  withComponentInputBinding,
  withNavigationErrorHandler,
  withRouterConfig,
} from '@angular/router';
import { routes } from './app.routes';
import { ManejadorErrores } from './nucleo/errores/manejador-errores';
import { manejarErrorNavegacion } from './nucleo/errores/manejar-error-navegacion';
import { interceptorErrores } from './nucleo/interceptores/interceptor-errores';
import { interceptorToken } from './nucleo/interceptores/interceptor-token';

registerLocaleData(localeEsEc);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: ErrorHandler, useClass: ManejadorErrores },
    { provide: LOCALE_ID, useValue: 'es-EC' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'USD' },
    provideRouter(
      routes,
      withComponentInputBinding(),
      withNavigationErrorHandler(manejarErrorNavegacion),
      withRouterConfig({ resolveNavigationPromiseOnError: true }),
    ),
    provideHttpClient(withInterceptors([interceptorErrores, interceptorToken])),
  ],
};
