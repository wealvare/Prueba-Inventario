import { Routes } from '@angular/router';

export const rutasAutenticacion: Routes = [
  {
    path: '',
    title: 'Iniciar sesión | Inventario',
    loadComponent: () =>
      import('./pagina-inicio-sesion/pagina-inicio-sesion').then((m) => m.PaginaInicioSesion),
  },
];
