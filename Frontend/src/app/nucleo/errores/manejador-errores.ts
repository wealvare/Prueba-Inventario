import { ErrorHandler, inject, Injectable } from '@angular/core';
import { ErrorApi } from '../modelos/error-api';
import { ServicioNotificaciones } from '../servicios/servicio-notificaciones';
import { ServicioRegistro } from '../servicios/servicio-registro';

@Injectable()
export class ManejadorErrores implements ErrorHandler {
  private readonly notificaciones = inject(ServicioNotificaciones);
  private readonly registro = inject(ServicioRegistro);

  handleError(error: unknown): void {
    if (error instanceof ErrorApi) {
      this.notificaciones.error(error.message);
      return;
    }

    this.registro.error('Error no controlado en la aplicación.', { error });
    this.notificaciones.error(
      'No se pudo completar la acción. Revisa tu conexión o recarga la página.',
    );
  }
}
