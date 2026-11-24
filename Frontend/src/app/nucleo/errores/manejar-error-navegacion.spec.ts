import { Component, ErrorHandler } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  provideRouter,
  Router,
  withNavigationErrorHandler,
  withRouterConfig,
} from '@angular/router';
import { ServicioNotificaciones } from '../servicios/servicio-notificaciones';
import { ServicioRegistro } from '../servicios/servicio-registro';
import { manejarErrorNavegacion } from './manejar-error-navegacion';

@Component({ template: '' })
class Pagina {}

describe('manejarErrorNavegacion', () => {
  it('registra una sola vez la ruta que no cargó y avisa en español', async () => {
    const registro = { error: vi.fn() };
    const manejador = { handleError: vi.fn() };
    const error = new TypeError('Failed to fetch dynamically imported module');
    TestBed.configureTestingModule({
      providers: [
        provideRouter(
          [
            { path: '', component: Pagina },
            { path: 'usuarios', loadComponent: () => Promise.reject(error) },
          ],
          withNavigationErrorHandler(manejarErrorNavegacion),
          withRouterConfig({ resolveNavigationPromiseOnError: true }),
        ),
        { provide: ServicioRegistro, useValue: registro },
        { provide: ErrorHandler, useValue: manejador },
      ],
    });

    expect(await TestBed.inject(Router).navigateByUrl('/usuarios')).toBe(false);

    expect(registro.error).toHaveBeenCalledExactlyOnceWith('No se pudo cargar la ruta.', {
      error,
      ruta: '/usuarios',
    });
    expect(manejador.handleError).not.toHaveBeenCalled();
    expect(TestBed.inject(ServicioNotificaciones).notificaciones()[0].mensaje).toBe(
      'No se pudo abrir la página. Revisa tu conexión y recarga para intentarlo de nuevo.',
    );
  });
});
