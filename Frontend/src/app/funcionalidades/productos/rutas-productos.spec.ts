import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { NEVER } from 'rxjs';
import { ServicioAutenticacion } from '../../nucleo/servicios/servicio-autenticacion';
import { rutasProductos } from './rutas-productos';
import { ServicioProductos } from './servicios/servicio-productos';

describe('Rutas de productos', () => {
  const visitar = async (administrador: boolean, ruta: string) => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'productos', children: rutasProductos }]),
        {
          provide: ServicioAutenticacion,
          useValue: { estaAutenticado: signal(true), esAdministrador: signal(administrador) },
        },
        { provide: ServicioProductos, useValue: { listar: () => NEVER, obtener: () => NEVER } },
      ],
    });
    await TestBed.inject(Router).navigateByUrl(ruta);
    return TestBed.inject(Router).url;
  };

  it.each(['/productos/nuevo', '/productos/7/editar'])(
    'el usuario de consulta no puede abrir %s',
    async (ruta) => {
      expect(await visitar(false, ruta)).toBe('/productos');
    },
  );

  it.each(['/productos/nuevo', '/productos/7/editar', '/productos/7'])(
    'el administrador puede abrir %s',
    async (ruta) => {
      expect(await visitar(true, ruta)).toBe(ruta);
    },
  );

  it('el usuario de consulta puede abrir el detalle', async () => {
    expect(await visitar(false, '/productos/7')).toBe('/productos/7');
  });
});
