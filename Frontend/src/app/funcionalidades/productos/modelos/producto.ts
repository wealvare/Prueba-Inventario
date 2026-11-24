export type EstadoProductos = 'Activos' | 'Inactivos' | 'Todos';

export interface FiltroProductos {
  pagina: number;
  tamanoPagina: number;
  busqueda: string;
  categoria: string;
  estado: EstadoProductos;
  precioMinimo: number | null;
  precioMaximo: number | null;
}

export interface ResultadoPaginado<T> {
  elementos: T[];
  pagina: number;
  tamanoPagina: number;
  totalElementos: number;
  totalPaginas: number;
}

export interface ProductoResumen {
  id: number;
  codigo: string;
  nombre: string;
  categoria: string;
  activo: boolean;
  cantidadLotes: number;
  cantidadTotal: number;
  precioMinimo: number | null;
  precioMaximo: number | null;
}

export interface Lote {
  id: number;
  numeroLote: string;
  fechaIngreso: string;
  precio: number;
  cantidad: number;
}

export interface Producto {
  id: number;
  codigo: string;
  nombre: string;
  descripcion: string;
  categoria: string;
  activo: boolean;
  lotes: Lote[];
}

export type SolicitudLote = Omit<Lote, 'id'> & { id: number | null };

export interface SolicitudCrearProducto {
  codigo: string;
  nombre: string;
  descripcion: string;
  categoria: string;
  lotes: SolicitudLote[];
}

export interface SolicitudActualizarProducto extends SolicitudCrearProducto {
  activo: boolean;
}
