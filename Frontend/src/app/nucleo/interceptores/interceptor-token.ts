import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ServicioAutenticacion, URL_INICIO_SESION } from '../servicios/servicio-autenticacion';
import { esUrlApi } from './es-url-api';

export const interceptorToken: HttpInterceptorFn = (peticion, siguiente) => {
  if (!esUrlApi(peticion.url) || peticion.url === URL_INICIO_SESION) {
    return siguiente(peticion);
  }

  const token = inject(ServicioAutenticacion).obtenerToken();

  return siguiente(
    token === null
      ? peticion
      : peticion.clone({ setHeaders: { Authorization: `Bearer ${token}` } }),
  );
};
