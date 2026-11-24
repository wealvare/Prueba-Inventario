import { Component, inject } from '@angular/core';
import { TipoNotificacion } from '../../nucleo/modelos/notificacion';
import { ServicioNotificaciones } from '../../nucleo/servicios/servicio-notificaciones';

const BORDES: Record<TipoNotificacion, string> = {
  exito: 'border-l-acento',
  informacion: 'border-l-neutral-400',
  error: 'border-l-red-700',
};

@Component({
  selector: 'app-notificaciones',
  templateUrl: './notificaciones.html',
})
export class Notificaciones {
  private readonly servicio = inject(ServicioNotificaciones);

  protected readonly notificaciones = this.servicio.notificaciones;
  protected readonly bordes = BORDES;

  protected cerrar(id: number): void {
    this.servicio.cerrar(id);
  }
}
