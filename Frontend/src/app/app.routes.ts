import { Routes } from '@angular/router';
import { guardiaAnonimo } from './nucleo/guardias/guardia-anonimo';
import { guardiaAutenticado } from './nucleo/guardias/guardia-autenticado';

export const routes: Routes = [
  {
    path: 'iniciar-sesion',
    canActivate: [guardiaAnonimo],
    loadChildren: () =>
      import('./funcionalidades/autenticacion/rutas-autenticacion').then(
        (m) => m.rutasAutenticacion,
      ),
  },
  {
    path: '',
    canActivate: [guardiaAutenticado],
    loadComponent: () => import('./nucleo/diseno/diseno-principal').then((m) => m.DisenoPrincipal),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'productos' },
      {
        path: 'productos',
        loadChildren: () =>
          import('./funcionalidades/productos/rutas-productos').then((m) => m.rutasProductos),
      },
    ],
  },
  { path: '**', redirectTo: 'productos' },
];
