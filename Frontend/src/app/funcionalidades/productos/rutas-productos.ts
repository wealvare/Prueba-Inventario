import { Routes } from '@angular/router';

export const rutasProductos: Routes = [
  {
    path: '',
    title: 'Productos | Inventario',
    loadComponent: () =>
      import('./pagina-productos/pagina-productos').then((m) => m.PaginaProductos),
  },
];
