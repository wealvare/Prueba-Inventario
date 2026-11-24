import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { crearToken } from '../../pruebas/token-prueba';
import { ErrorApi } from '../modelos/error-api';
import { ServicioAutenticacion, URL_INICIO_SESION } from './servicio-autenticacion';
import { ServicioNotificaciones } from './servicio-notificaciones';

const CLAVE = 'inventario.token';

describe('ServicioAutenticacion', () => {
  const crear = () => TestBed.inject(ServicioAutenticacion);

  const iniciar = (servicio: ServicioAutenticacion, token: string) => {
    servicio.iniciarSesion({ nombreUsuario: 'admin', contrasena: 'clave' }).subscribe();
    TestBed.inject(HttpTestingController).expectOne(URL_INICIO_SESION).flush({ token });
  };

  beforeEach(() => {
    vi.useFakeTimers();
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('empieza sin sesión', () => {
    const servicio = crear();

    expect(servicio.estaAutenticado()).toBe(false);
    expect(servicio.esAdministrador()).toBe(false);
    expect(servicio.usuarioActual()).toBeNull();
    expect(servicio.obtenerToken()).toBeNull();
  });

  it('guarda el token y expone el usuario al iniciar sesión', () => {
    const servicio = crear();
    const token = crearToken('Administrador', 3600, 'Íñigo Muñoz');

    iniciar(servicio, token);

    expect(servicio.estaAutenticado()).toBe(true);
    expect(servicio.esAdministrador()).toBe(true);
    expect(servicio.usuarioActual()?.nombreUsuario).toBe('Íñigo Muñoz');
    expect(servicio.obtenerToken()).toBe(token);
    expect(sessionStorage.getItem(CLAVE)).toBe(token);
  });

  it('no considera administrador al rol Consulta', () => {
    const servicio = crear();

    iniciar(servicio, crearToken('Consulta'));

    expect(servicio.estaAutenticado()).toBe(true);
    expect(servicio.esAdministrador()).toBe(false);
  });

  it.each(['texto', 'a.b.c', crearToken('Otro' as never), crearToken('Consulta', -1)])(
    'rechaza un token recibido que no es válido',
    (token) => {
      const servicio = crear();
      let error: unknown;

      servicio
        .iniciarSesion({ nombreUsuario: 'admin', contrasena: 'clave' })
        .subscribe({ error: (e) => (error = e) });
      TestBed.inject(HttpTestingController).expectOne(URL_INICIO_SESION).flush({ token });

      expect(error).toBeInstanceOf(ErrorApi);
      expect(servicio.estaAutenticado()).toBe(false);
      expect(sessionStorage.getItem(CLAVE)).toBeNull();
    },
  );

  it('recupera una sesión guardada', () => {
    sessionStorage.setItem(CLAVE, crearToken('Consulta', 3600, 'consulta'));

    expect(crear().usuarioActual()?.nombreUsuario).toBe('consulta');
  });

  it('descarta una sesión guardada que ya expiró', () => {
    sessionStorage.setItem(CLAVE, crearToken('Consulta', -1));

    expect(crear().estaAutenticado()).toBe(false);
    expect(sessionStorage.getItem(CLAVE)).toBeNull();
  });

  it('cierra la sesión y va al inicio de sesión', () => {
    const servicio = crear();
    iniciar(servicio, crearToken());

    servicio.cerrarSesion();

    expect(servicio.estaAutenticado()).toBe(false);
    expect(sessionStorage.getItem(CLAVE)).toBeNull();
    expect(TestBed.inject(Router).navigateByUrl).toHaveBeenCalledWith('/iniciar-sesion');
  });

  it('cierra la sesión automáticamente cuando el token expira', () => {
    const servicio = crear();
    iniciar(servicio, crearToken('Consulta', 60));

    vi.advanceTimersByTime(58_000);
    expect(servicio.estaAutenticado()).toBe(true);
    vi.advanceTimersByTime(3_000);

    expect(servicio.estaAutenticado()).toBe(false);
    expect(TestBed.inject(ServicioNotificaciones).notificaciones()[0].mensaje).toBe(
      'Tu sesión terminó. Inicia sesión de nuevo.',
    );
    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith(['/iniciar-sesion'], {
      queryParams: { retorno: '/' },
    });
  });

  it('no entrega un token vencido aunque el temporizador no se haya ejecutado', () => {
    const servicio = crear();
    iniciar(servicio, crearToken('Consulta', 60));

    vi.setSystemTime(Date.now() + 120_000);

    expect(servicio.obtenerToken()).toBeNull();
    expect(servicio.estaAutenticado()).toBe(false);
  });

  it('no cierra la sesión antes de tiempo con expiraciones muy largas', () => {
    const servicio = crear();
    iniciar(servicio, crearToken('Consulta', 60 * 24 * 3600));

    vi.advanceTimersByTime(30 * 24 * 3600 * 1000);

    expect(servicio.estaAutenticado()).toBe(true);
  });

  it('cancela la expiración anterior al iniciar otra sesión', () => {
    const servicio = crear();
    iniciar(servicio, crearToken('Consulta', 60));
    servicio.cerrarSesion();
    iniciar(servicio, crearToken('Administrador', 3600));

    vi.advanceTimersByTime(120_000);

    expect(servicio.esAdministrador()).toBe(true);
  });
});
