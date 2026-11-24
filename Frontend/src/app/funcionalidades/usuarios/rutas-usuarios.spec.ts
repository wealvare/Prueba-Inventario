import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { NEVER } from 'rxjs';
import { routes } from '../../app.routes';
import { ServicioAutenticacion } from '../../nucleo/servicios/servicio-autenticacion';
import { ServicioNotificaciones } from '../../nucleo/servicios/servicio-notificaciones';
import { ServicioUsuarios } from './servicios/servicio-usuarios';

describe('Rutas de usuarios', () => {
  const visitar = async (ruta: string, administrador: boolean, autenticado = true) => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        {
          provide: ServicioAutenticacion,
          useValue: {
            estaAutenticado: signal(autenticado),
            esAdministrador: signal(administrador),
            usuarioActual: signal(null),
          },
        },
        { provide: ServicioUsuarios, useValue: { listar: () => NEVER, obtener: () => NEVER } },
      ],
    });
    await TestBed.inject(Router).navigateByUrl(ruta);
    return TestBed.inject(Router).url;
  };

  const RUTAS = ['/usuarios', '/usuarios/nuevo', '/usuarios/7/editar'];

  it.each(RUTAS)('el administrador puede abrir %s', async (ruta) => {
    expect(await visitar(ruta, true)).toBe(ruta);
  });

  it.each(RUTAS)('el usuario de consulta no puede abrir %s', async (ruta) => {
    expect(await visitar(ruta, false)).toBe('/productos');
    expect(TestBed.inject(ServicioNotificaciones).notificaciones()[0].mensaje).toBe(
      'No tienes permiso para acceder a esta página.',
    );
  });

  it('sin sesión pide iniciarla', async () => {
    expect(await visitar('/usuarios', false, false)).toBe('/iniciar-sesion?retorno=%2Fusuarios');
  });

  it('con sesión no se puede abrir la creación de cuentas', async () => {
    expect(await visitar('/crear-cuenta', false)).toBe('/productos');
  });

  it('sin sesión se puede abrir la creación de cuentas', async () => {
    expect(await visitar('/crear-cuenta', false, false)).toBe('/crear-cuenta');
  });
});
