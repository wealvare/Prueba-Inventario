import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { RUTA_INICIO_SESION, ServicioAutenticacion } from '../servicios/servicio-autenticacion';

export const guardiaAutenticado: CanActivateFn = (_ruta, estado) =>
  inject(ServicioAutenticacion).estaAutenticado() ||
  inject(Router).createUrlTree([RUTA_INICIO_SESION], { queryParams: { retorno: estado.url } });
