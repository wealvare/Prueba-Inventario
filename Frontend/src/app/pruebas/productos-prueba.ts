import { ResultadoPaginado } from '../compartido/modelos/resultado-paginado';
import { Producto, ProductoResumen } from '../funcionalidades/productos/modelos/producto';

export const PRODUCTO: Producto = {
  id: 7,
  codigo: 'TEC-1',
  nombre: 'Teclado',
  descripcion: 'Mecánico',
  categoria: 'Periféricos',
  activo: true,
  lotes: [
    { id: 1, numeroLote: 'L-1', fechaIngreso: '2026-01-15', precio: 10, cantidad: 5 },
    { id: 2, numeroLote: 'L-2', fechaIngreso: '2026-02-15', precio: 12.5, cantidad: 0 },
  ],
};

export function crearPagina(
  cambios: Partial<ResultadoPaginado<ProductoResumen>> = {},
): ResultadoPaginado<ProductoResumen> {
  return {
    elementos: [
      {
        id: 7,
        codigo: 'TEC-1',
        nombre: 'Teclado',
        categoria: 'Periféricos',
        activo: true,
        cantidadLotes: 2,
        cantidadTotal: 1500,
        precioMinimo: 10,
        precioMaximo: 12.5,
      },
    ],
    pagina: 1,
    tamanoPagina: 10,
    totalElementos: 25,
    totalPaginas: 3,
    ...cambios,
  };
}
