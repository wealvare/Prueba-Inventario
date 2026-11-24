import { Routes } from '@angular/router';

export const rutasUsuarios: Routes = [
  {
    path: '',
    title: 'Usuarios | Inventario',
    loadComponent: () => import('./pagina-usuarios/pagina-usuarios').then((m) => m.PaginaUsuarios),
  },
  {
    path: 'nuevo',
    title: 'Nuevo usuario | Inventario',
    loadComponent: () =>
      import('./pagina-formulario-usuario/pagina-formulario-usuario').then(
        (m) => m.PaginaFormularioUsuario,
      ),
  },
  {
    path: ':id/editar',
    title: 'Editar usuario | Inventario',
    loadComponent: () =>
      import('./pagina-formulario-usuario/pagina-formulario-usuario').then(
        (m) => m.PaginaFormularioUsuario,
      ),
  },
];
