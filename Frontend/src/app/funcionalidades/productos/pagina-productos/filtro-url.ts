import { ParamMap, Params } from '@angular/router';
import { EstadoProductos, FiltroProductos } from '../modelos/producto';

export const ESTADOS: readonly EstadoProductos[] = ['Activos', 'Inactivos', 'Todos'];
export const TAMANOS_PAGINA: readonly number[] = [10, 20, 50];

export const FILTRO_INICIAL: FiltroProductos = {
  pagina: 1,
  tamanoPagina: 10,
  busqueda: '',
  categoria: '',
  estado: 'Activos',
  precioMinimo: null,
  precioMaximo: null,
};

export function leerFiltro(parametros: ParamMap): FiltroProductos {
  const pagina = Number(parametros.get('pagina'));
  const tamanoPagina = Number(parametros.get('tamanoPagina'));
  const estado = parametros.get('estado') as EstadoProductos;

  return {
    pagina: Number.isInteger(pagina) && pagina > 0 ? pagina : FILTRO_INICIAL.pagina,
    tamanoPagina: TAMANOS_PAGINA.includes(tamanoPagina)
      ? tamanoPagina
      : FILTRO_INICIAL.tamanoPagina,
    busqueda: parametros.get('busqueda') ?? '',
    categoria: parametros.get('categoria') ?? '',
    estado: ESTADOS.includes(estado) ? estado : FILTRO_INICIAL.estado,
    precioMinimo: leerPrecio(parametros.get('precioMinimo')),
    precioMaximo: leerPrecio(parametros.get('precioMaximo')),
  };
}

export function escribirFiltro(filtro: FiltroProductos): Params {
  return Object.fromEntries(
    Object.entries(filtro).filter(
      ([clave, valor]) => valor !== FILTRO_INICIAL[clave as keyof FiltroProductos],
    ),
  );
}

function leerPrecio(texto: string | null): number | null {
  const precio = Number(texto);
  return texto !== null && texto !== '' && Number.isFinite(precio) && precio >= 0 ? precio : null;
}
