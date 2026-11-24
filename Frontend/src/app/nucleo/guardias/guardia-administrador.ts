import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { RUTA_INICIO_SESION, ServicioAutenticacion } from '../servicios/servicio-autenticacion';
import { ServicioNotificaciones } from '../servicios/servicio-notificaciones';

export const guardiaAdministrador: CanActivateFn = (_ruta, estado) => {
  const autenticacion = inject(ServicioAutenticacion);
  const router = inject(Router);

  if (!autenticacion.estaAutenticado()) {
    return router.createUrlTree([RUTA_INICIO_SESION], { queryParams: { retorno: estado.url } });
  }

  if (autenticacion.esAdministrador()) {
    return true;
  }

  inject(ServicioNotificaciones).error('No tienes permiso para acceder a esta página.');
  return router.createUrlTree(['/productos']);
};
