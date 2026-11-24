import { HttpContext, HttpContextToken } from '@angular/common/http';

export const ERRORES_EN_PANTALLA = new HttpContextToken<(estado: number) => boolean>(
  () => () => false,
);

export function mostrarEnPantalla(...estados: number[]): HttpContext {
  return new HttpContext().set(
    ERRORES_EN_PANTALLA,
    (estado) => estados.length === 0 || estados.includes(estado),
  );
}
