import { Routes } from '@angular/router';
import { guardiaAdministrador } from '../../nucleo/guardias/guardia-administrador';

export const rutasProductos: Routes = [
  {
    path: '',
    title: 'Productos | Inventario',
    loadComponent: () =>
      import('./pagina-productos/pagina-productos').then((m) => m.PaginaProductos),
  },
  {
    path: 'nuevo',
    title: 'Nuevo producto | Inventario',
    canActivate: [guardiaAdministrador],
    loadComponent: () =>
      import('./pagina-formulario-producto/pagina-formulario-producto').then(
        (m) => m.PaginaFormularioProducto,
      ),
  },
  {
    path: ':id',
    title: 'Detalle del producto | Inventario',
    loadComponent: () =>
      import('./pagina-detalle-producto/pagina-detalle-producto').then(
        (m) => m.PaginaDetalleProducto,
      ),
  },
  {
    path: ':id/editar',
    title: 'Editar producto | Inventario',
    canActivate: [guardiaAdministrador],
    loadComponent: () =>
      import('./pagina-formulario-producto/pagina-formulario-producto').then(
        (m) => m.PaginaFormularioProducto,
      ),
  },
];
