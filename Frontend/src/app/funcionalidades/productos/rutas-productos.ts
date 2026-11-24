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
      import('./pagina-nuevo-producto/pagina-nuevo-producto').then((m) => m.PaginaNuevoProducto),
  },
];
