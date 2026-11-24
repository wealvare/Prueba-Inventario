import { Injectable, signal } from '@angular/core';
import { Notificacion, TipoNotificacion } from '../modelos/notificacion';

const DURACION_MS: Record<TipoNotificacion, number> = {
  exito: 5000,
  informacion: 6000,
  error: 10000,
};

@Injectable({ providedIn: 'root' })
export class ServicioNotificaciones {
  private readonly lista = signal<Notificacion[]>([]);
  private siguienteId = 1;

  readonly notificaciones = this.lista.asReadonly();

  exito(mensaje: string): void {
    this.mostrar('exito', mensaje);
  }

  informacion(mensaje: string): void {
    this.mostrar('informacion', mensaje);
  }

  error(mensaje: string, correlacion?: string): void {
    this.mostrar('error', mensaje, correlacion);
  }

  cerrar(id: number): void {
    this.lista.update((actuales) => actuales.filter((notificacion) => notificacion.id !== id));
  }

  private mostrar(tipo: TipoNotificacion, mensaje: string, correlacion?: string): void {
    const id = this.siguienteId++;

    this.lista.update((actuales) => [
      ...actuales.filter(
        (notificacion) => notificacion.tipo !== tipo || notificacion.mensaje !== mensaje,
      ),
      { id, tipo, mensaje, correlacion },
    ]);
    setTimeout(() => this.cerrar(id), DURACION_MS[tipo]);
  }
}
