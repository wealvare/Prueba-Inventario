import { registerLocaleData } from '@angular/common';
import localeEsEc from '@angular/common/locales/es-EC';
import { LOCALE_ID, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ServicioAutenticacion } from '../../../nucleo/servicios/servicio-autenticacion';
import { PRODUCTO } from '../../../pruebas/productos-prueba';
import { ServicioProductos } from '../servicios/servicio-productos';
import { PaginaDetalleProducto } from './pagina-detalle-producto';

describe('PaginaDetalleProducto', () => {
  const preparar = async (administrador: boolean, activo = true) => {
    registerLocaleData(localeEsEc);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'es-EC' },
        { provide: ServicioProductos, useValue: { obtener: () => of({ ...PRODUCTO, activo }) } },
        { provide: ServicioAutenticacion, useValue: { esAdministrador: signal(administrador) } },
      ],
    });
    const fixture = TestBed.createComponent(PaginaDetalleProducto);
    fixture.componentRef.setInput('id', '7');
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  };

  it('muestra los datos y el precio de cada lote por fecha', async () => {
    const elemento = await preparar(true);

    const filas = [...elemento.querySelectorAll('tbody tr')].map((fila) =>
      [...fila.querySelectorAll('td')].map((celda) => celda.textContent?.trim()),
    );
    expect(elemento.querySelector('h1')?.textContent).toContain('Teclado');
    expect(filas).toEqual([
      ['15/01/2026', 'L-1', '$10,00', '5'],
      ['15/02/2026', 'L-2', '$12,50', '0'],
    ]);
  });

  it('el administrador puede editar y eliminar', async () => {
    const elemento = await preparar(true);

    expect(elemento.querySelector('a[href="/productos/7/editar"]')).not.toBeNull();
    expect(elemento.querySelector('.boton-peligro')).not.toBeNull();
  });

  it('un producto inactivo no se puede volver a eliminar', async () => {
    const elemento = await preparar(true, false);

    expect(elemento.querySelector('a[href="/productos/7/editar"]')).not.toBeNull();
    expect(elemento.querySelector('.boton-peligro')).toBeNull();
  });

  it('el usuario de consulta no ve acciones', async () => {
    const elemento = await preparar(false);

    expect(elemento.querySelector('a[href="/productos/7/editar"]')).toBeNull();
    expect(elemento.querySelector('.boton-peligro')).toBeNull();
  });
});
