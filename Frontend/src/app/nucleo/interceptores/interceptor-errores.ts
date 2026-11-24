import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { esRespuestaInvalida, traducirError } from '../errores/traducir-error';
import { ServicioAutenticacion, URL_INICIO_SESION } from '../servicios/servicio-autenticacion';
import { ServicioNotificaciones } from '../servicios/servicio-notificaciones';
import { ServicioRegistro } from '../servicios/servicio-registro';
import { ERRORES_EN_PANTALLA } from './errores-en-pantalla';
import { esUrlApi } from './es-url-api';

const ADVERTENCIAS_ESPERADAS = [400, 401, 404, 409];

export const interceptorErrores: HttpInterceptorFn = (peticion, siguiente) => {
  if (!esUrlApi(peticion.url)) {
    return siguiente(peticion);
  }

  const autenticacion = inject(ServicioAutenticacion);
  const notificaciones = inject(ServicioNotificaciones);
  const registro = inject(ServicioRegistro);

  return siguiente(peticion).pipe(
    catchError((respuesta: unknown) => {
      if (!(respuesta instanceof HttpErrorResponse)) {
        return throwError(() => respuesta);
      }

      const error = traducirError(respuesta);
      const datos = {
        metodo: peticion.method,
        url: peticion.url,
        estado: error.estado,
        correlacion: error.correlacion,
      };

      if (error.estado === 0 || error.estado >= 500 || esRespuestaInvalida(error.estado)) {
        registro.error(error.message, datos);
      } else if (ADVERTENCIAS_ESPERADAS.includes(error.estado)) {
        registro.advertencia(error.message, datos);
      } else {
        registro.advertenciaImportante(error.message, datos);
      }

      if (error.estado === 401 && peticion.url !== URL_INICIO_SESION) {
        autenticacion.expirarSesion();
      } else if (!peticion.context.get(ERRORES_EN_PANTALLA)(error.estado)) {
        notificaciones.error(error.message, error.estado >= 500 ? error.correlacion : undefined);
      }

      return throwError(() => error);
    }),
  );
};
