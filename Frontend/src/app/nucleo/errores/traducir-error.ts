import { HttpErrorResponse } from '@angular/common/http';
import { ErrorApi, ProblemaApi } from '../modelos/error-api';

const MENSAJES: Record<number, string> = {
  0: 'No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.',
  400: 'Revisa los datos enviados.',
  401: 'Debes iniciar sesión para continuar.',
  403: 'No tienes permiso para realizar esta acción.',
  404: 'El recurso solicitado no existe.',
  409: 'La operación entra en conflicto con los datos actuales.',
  429: 'Demasiadas peticiones. Espera un momento antes de volver a intentarlo.',
  500: 'Ocurrió un error inesperado. Inténtalo de nuevo más tarde.',
};

const ESTADOS_CON_DETALLE = [400, 401, 403, 404, 409];

export function traducirError(respuesta: HttpErrorResponse): ErrorApi {
  const problema = leerProblema(respuesta.error);
  const correlacion = problema.correlacion ?? respuesta.headers.get('X-Correlacion') ?? undefined;

  return new ErrorApi(
    respuesta.status,
    elegirMensaje(respuesta, problema),
    correlacion,
    problema.errors,
  );
}

function leerProblema(cuerpo: unknown): ProblemaApi {
  return typeof cuerpo === 'object' && cuerpo !== null && !(cuerpo instanceof Event)
    ? (cuerpo as ProblemaApi)
    : {};
}

function elegirMensaje(respuesta: HttpErrorResponse, problema: ProblemaApi): string {
  const { status: estado } = respuesta;

  if (ESTADOS_CON_DETALLE.includes(estado) && problema.detail) {
    return problema.detail;
  }

  if (estado === 429) {
    const segundos = Number(respuesta.headers.get('Retry-After'));

    if (segundos > 0) {
      return `Demasiadas peticiones. Vuelve a intentarlo en ${segundos} segundos.`;
    }
  }

  return MENSAJES[estado] ?? MENSAJES[500];
}
