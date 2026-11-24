import { entorno } from '../../../entornos/entorno';

export function esUrlApi(url: string): boolean {
  return url.startsWith(`${entorno.urlApi}/`);
}
