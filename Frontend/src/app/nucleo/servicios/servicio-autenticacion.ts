import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { entorno } from '../../../entornos/entorno';
import { RespuestaInicioSesion, SolicitudInicioSesion, Usuario } from '../modelos/sesion';
import { ErrorApi } from '../modelos/error-api';
import { AlmacenToken } from './almacen-token';
import { leerToken } from './lector-token';
import { ServicioNotificaciones } from './servicio-notificaciones';

export const RUTA_INICIO_SESION = '/iniciar-sesion';
export const URL_INICIO_SESION = `${entorno.urlApi}/api/autenticacion/login`;
const ESPERA_MAXIMA_MS = 2_147_483_647;

@Injectable({ providedIn: 'root' })
export class ServicioAutenticacion {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly almacen = inject(AlmacenToken);
  private readonly notificaciones = inject(ServicioNotificaciones);

  private readonly tokenActual = signal<string | null>(null);
  private readonly usuario = signal<Usuario | null>(null);
  private temporizador?: ReturnType<typeof setTimeout>;

  readonly usuarioActual = this.usuario.asReadonly();
  readonly estaAutenticado = computed(() => this.usuario() !== null);
  readonly esAdministrador = computed(() => this.usuario()?.rol === 'Administrador');

  constructor() {
    const token = this.almacen.leer();

    if (token !== null && !this.establecer(token)) {
      this.almacen.borrar();
    }
  }

  iniciarSesion(solicitud: SolicitudInicioSesion): Observable<RespuestaInicioSesion> {
    return this.http.post<RespuestaInicioSesion>(URL_INICIO_SESION, solicitud).pipe(
      tap(({ token }) => {
        if (!this.establecer(token)) {
          const mensaje = 'No se pudo iniciar la sesión. Revisa la fecha y hora de tu equipo.';
          this.notificaciones.error(mensaje);
          throw new ErrorApi(0, mensaje);
        }

        this.almacen.guardar(token);
      }),
    );
  }

  obtenerToken(): string | null {
    this.verificarVigencia();
    return this.tokenActual();
  }

  verificarVigencia(): boolean {
    const usuario = this.usuario();

    if (usuario !== null && usuario.expiracion <= Date.now()) {
      this.expirarSesion();
    }

    return this.estaAutenticado();
  }

  cerrarSesion(): void {
    this.limpiar();
    void this.router.navigateByUrl(RUTA_INICIO_SESION);
  }

  expirarSesion(): void {
    this.limpiar();
    this.notificaciones.informacion('Tu sesión terminó. Inicia sesión de nuevo.');

    if (!this.router.url.startsWith(RUTA_INICIO_SESION)) {
      void this.router.navigate([RUTA_INICIO_SESION], {
        queryParams: { retorno: this.router.url },
      });
    }
  }

  private establecer(token: string): boolean {
    const usuario = leerToken(token);

    if (usuario === null || usuario.expiracion <= Date.now()) {
      return false;
    }

    this.tokenActual.set(token);
    this.usuario.set(usuario);
    this.programarExpiracion(usuario.expiracion);
    return true;
  }

  private programarExpiracion(expiracion: number): void {
    clearTimeout(this.temporizador);
    const espera = Math.min(Math.max(expiracion - Date.now(), 0), ESPERA_MAXIMA_MS);

    this.temporizador = setTimeout(() => {
      if (this.verificarVigencia()) {
        this.programarExpiracion(expiracion);
      }
    }, espera);
  }

  private limpiar(): void {
    clearTimeout(this.temporizador);
    this.almacen.borrar();
    this.tokenActual.set(null);
    this.usuario.set(null);
  }
}
