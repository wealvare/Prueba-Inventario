import { inject } from '@angular/core';
import { NavigationError } from '@angular/router';
import { ServicioNotificaciones } from '../servicios/servicio-notificaciones';
import { ServicioRegistro } from '../servicios/servicio-registro';

export function manejarErrorNavegacion({ error, url }: NavigationError): void {
  inject(ServicioRegistro).error('No se pudo cargar la ruta.', { error, ruta: url });
  inject(ServicioNotificaciones).error(
    'No se pudo abrir la página. Revisa tu conexión y recarga para intentarlo de nuevo.',
  );
}
