const RUTA_POR_DEFECTO = '/productos';

export function rutaSegura(retorno: string | undefined): string {
  return retorno && /^\/(?![/\\])/.test(retorno) && !retorno.startsWith('/iniciar-sesion')
    ? retorno
    : RUTA_POR_DEFECTO;
}
