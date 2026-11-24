import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ServicioAutenticacion } from '../servicios/servicio-autenticacion';

export const guardiaAnonimo: CanActivateFn = () =>
  !inject(ServicioAutenticacion).estaAutenticado() || inject(Router).createUrlTree(['/productos']);
