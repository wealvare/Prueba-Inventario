import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  provideRouter,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { ServicioAutenticacion } from '../servicios/servicio-autenticacion';
import { ServicioNotificaciones } from '../servicios/servicio-notificaciones';
import { guardiaAdministrador } from './guardia-administrador';
import { guardiaAnonimo } from './guardia-anonimo';
import { guardiaAutenticado } from './guardia-autenticado';

describe('Guardias', () => {
  const estaAutenticado = signal(false);
  const esAdministrador = signal(false);

  const ejecutar = (guardia: CanActivateFn) => {
    const resultado = TestBed.runInInjectionContext(() =>
      guardia({} as ActivatedRouteSnapshot, { url: '/productos/nuevo?a=1' } as RouterStateSnapshot),
    );
    return resultado instanceof UrlTree ? resultado.toString() : resultado;
  };

  beforeEach(() => {
    estaAutenticado.set(false);
    esAdministrador.set(false);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: ServicioAutenticacion, useValue: { estaAutenticado, esAdministrador } },
      ],
    });
  });

  it.each([guardiaAutenticado, guardiaAdministrador])(
    'sin sesión redirige al inicio de sesión y conserva la ruta',
    (guardia) => {
      expect(ejecutar(guardia)).toBe('/iniciar-sesion?retorno=%2Fproductos%2Fnuevo%3Fa%3D1');
    },
  );

  it('la guardia de autenticados permite el paso con sesión', () => {
    estaAutenticado.set(true);

    expect(ejecutar(guardiaAutenticado)).toBe(true);
  });

  it('la guardia de administradores permite el paso al administrador', () => {
    estaAutenticado.set(true);
    esAdministrador.set(true);

    expect(ejecutar(guardiaAdministrador)).toBe(true);
  });

  it('la guardia de administradores devuelve a productos a los demás roles y avisa', () => {
    estaAutenticado.set(true);

    expect(ejecutar(guardiaAdministrador)).toBe('/productos');
    expect(TestBed.inject(ServicioNotificaciones).notificaciones()[0].mensaje).toBe(
      'No tienes permiso para acceder a esta página.',
    );
  });

  it('la guardia anónima solo permite el paso sin sesión', () => {
    expect(ejecutar(guardiaAnonimo)).toBe(true);

    estaAutenticado.set(true);

    expect(ejecutar(guardiaAnonimo)).toBe('/productos');
  });
});
