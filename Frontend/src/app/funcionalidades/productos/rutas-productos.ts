import { Routes } from '@angular/router';

export const rutasProductos: Routes = [
  {
    path: '',
    title: 'Productos | Inventario',
    loadComponent: () =>
      import('./pagina-productos/pagina-productos').then((m) => m.PaginaProductos),
  },
  {
    path: ':id',
    title: 'Detalle del producto | Inventario',
    loadComponent: () =>
      import('./pagina-detalle-producto/pagina-detalle-producto').then(
        (m) => m.PaginaDetalleProducto,
      ),
  },
];
