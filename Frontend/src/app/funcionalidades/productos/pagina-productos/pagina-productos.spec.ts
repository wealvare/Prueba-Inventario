import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import localeEsCo from '@angular/common/locales/es-CO';
import { LOCALE_ID, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { entorno } from '../../../../entornos/entorno';
import { interceptorErrores } from '../../../nucleo/interceptores/interceptor-errores';
import { ServicioAutenticacion } from '../../../nucleo/servicios/servicio-autenticacion';
import { ServicioNotificaciones } from '../../../nucleo/servicios/servicio-notificaciones';
import { ServicioRegistro } from '../../../nucleo/servicios/servicio-registro';
import { prepararDialogo } from '../../../pruebas/dialogo-prueba';
import { crearPagina } from '../../../pruebas/productos-prueba';
import { PaginaProductos } from './pagina-productos';

const URL = `${entorno.urlApi}/productos`;
const RETARDO = 450;

describe('PaginaProductos', () => {
  const esAdministrador = signal(true);

  const esperar = (ms = 0) => new Promise((resolver) => setTimeout(resolver, ms));

  const pendiente = (): TestRequest => {
    TestBed.tick();
    return TestBed.inject(HttpTestingController).expectOne((peticion) => peticion.url === URL);
  };

  const pendienteCategorias = (): TestRequest =>
    TestBed.inject(HttpTestingController).expectOne(`${URL}/categorias`);

  const preparar = async (
    ruta = '/productos',
    categorias: string[] | null = ['Periféricos', 'Redes'],
  ) => {
    prepararDialogo();
    registerLocaleData(localeEsCo);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'productos', component: PaginaProductos }]),
        provideHttpClient(withInterceptors([interceptorErrores])),
        provideHttpClientTesting(),
        { provide: LOCALE_ID, useValue: 'es-CO' },
        { provide: ServicioAutenticacion, useValue: { esAdministrador, expirarSesion: vi.fn() } },
        { provide: ServicioRegistro, useValue: { advertencia: vi.fn(), error: vi.fn() } },
      ],
    });
    const arnes = await RouterTestingHarness.create();
    void arnes.navigateByUrl(ruta);
    await esperar();
    const peticion = pendienteCategorias();

    if (categorias === null) {
      peticion.flush(null, { status: 500, statusText: 'Error' });
    } else {
      peticion.flush(categorias);
    }

    await esperar();
    const elemento = arnes.fixture.nativeElement as HTMLElement;

    const responder = async (peticion: TestRequest, datos: object = crearPagina()) => {
      peticion.flush(datos);
      await esperar();
      TestBed.tick();
      await esperar();
    };
    const escribir = (id: string, valor: string) => {
      const campo = elemento.querySelector<HTMLInputElement>(`#${id}`)!;
      campo.value = valor;
      campo.dispatchEvent(new Event(campo.tagName === 'SELECT' ? 'change' : 'input'));
    };
    const boton = (texto: string) =>
      [...elemento.querySelectorAll('button')].find((b) => b.textContent?.trim() === texto)!;

    return { elemento, responder, escribir, boton };
  };

  beforeEach(() => esAdministrador.set(true));

  it('pide la primera página de activos y muestra los productos', async () => {
    const { elemento, responder } = await preparar();
    const peticion = pendiente();
    expect(elemento.textContent).toContain('Cargando productos…');
    expect(peticion.request.params.toString()).toBe('pagina=1&tamanoPagina=10&estado=Activos');

    await responder(peticion);

    const fila = elemento.querySelector('tbody tr')?.textContent;
    expect(fila).toContain('TEC-1');
    expect(fila).toContain('1.500');
    expect(fila).toContain('12,50');
    expect(elemento.querySelector('app-paginador')?.textContent).toContain('Página 1 de 3');
  });

  it('aplica los filtros con retardo en una sola petición y vuelve a la primera página', async () => {
    const { responder, escribir } = await preparar('/productos?pagina=2');
    await responder(pendiente(), crearPagina({ pagina: 2 }));

    escribir('busqueda', 'te');
    escribir('busqueda', ' tec ');
    escribir('categoria', 'Periféricos');
    escribir('estado', 'Todos');
    escribir('precioMinimo', '5');
    escribir('precioMaximo', '20.5');
    TestBed.inject(HttpTestingController).verify();
    await esperar(RETARDO);

    const { params } = pendiente().request;
    expect(Object.fromEntries(params.keys().map((clave) => [clave, params.get(clave)]))).toEqual({
      pagina: '1',
      tamanoPagina: '10',
      busqueda: 'tec',
      categoria: 'Periféricos',
      estado: 'Todos',
      precioMinimo: '5',
      precioMaximo: '20.5',
    });
    expect(decodeURIComponent(TestBed.inject(Router).url)).toContain('busqueda=tec');
  });

  it.each([
    ['20', '5', 'El precio máximo no puede ser menor que el mínimo.'],
    ['-1', '5', 'Los precios no pueden ser negativos.'],
  ])('no consulta con el rango de precios %s a %s', async (minimo, maximo, mensaje) => {
    const { elemento, responder, escribir } = await preparar();
    await responder(pendiente());

    escribir('precioMinimo', minimo);
    escribir('precioMaximo', maximo);
    await esperar(RETARDO);
    TestBed.tick();

    TestBed.inject(HttpTestingController).verify();
    expect(elemento.querySelector('#error-precios')?.textContent).toContain(mensaje);
  });

  it('restaura los filtros desde la URL', async () => {
    const { elemento } = await preparar('/productos?busqueda=rat&estado=Inactivos&tamanoPagina=20');

    expect(pendiente().request.params.get('estado')).toBe('Inactivos');
    expect(elemento.querySelector<HTMLInputElement>('#busqueda')?.value).toBe('rat');
    expect(elemento.querySelector<HTMLSelectElement>('#estado')?.value).toBe('Inactivos');
  });

  it('ofrece las categorías y conserva la de la URL aunque ya no exista', async () => {
    const { elemento } = await preparar('/productos?categoria=Antigua');
    const categoria = elemento.querySelector<HTMLSelectElement>('#categoria')!;

    expect(pendiente().request.params.get('categoria')).toBe('Antigua');
    expect([...categoria.options].map(({ text }) => text)).toEqual([
      'Todas',
      'Antigua',
      'Periféricos',
      'Redes',
    ]);
    expect(categoria.value).toBe('Antigua');
  });

  it('informa si no se pueden cargar las categorías', async () => {
    const { elemento, responder } = await preparar('/productos', null);
    await responder(pendiente());

    expect(elemento.querySelector('#categoria-error')?.textContent).toContain(
      'No se pudieron cargar las categorías.',
    );
    expect(elemento.querySelector('tbody tr')).not.toBeNull();
  });

  it('cambia de página conservando los filtros', async () => {
    const { responder, boton } = await preparar('/productos?estado=Todos');
    await responder(pendiente());
    expect(boton('Anterior').disabled).toBe(true);

    boton('Siguiente').click();
    await esperar();

    const { params } = pendiente().request;
    expect([params.get('pagina'), params.get('estado')]).toEqual(['2', 'Todos']);
  });

  it('va a la última página si la solicitada ya no existe', async () => {
    const { responder } = await preparar('/productos?pagina=9');

    await responder(pendiente(), crearPagina({ elementos: [], pagina: 9 }));

    expect(pendiente().request.params.get('pagina')).toBe('3');
  });

  it('limpia los filtros', async () => {
    const { responder, boton } = await preparar('/productos?busqueda=x&estado=Todos');
    await responder(pendiente());

    boton('Limpiar filtros').click();
    await esperar(RETARDO);

    expect(pendiente().request.params.toString()).toBe('pagina=1&tamanoPagina=10&estado=Activos');
  });

  it('informa cuando no hay resultados', async () => {
    const { elemento, responder } = await preparar();

    await responder(
      pendiente(),
      crearPagina({ elementos: [], totalElementos: 0, totalPaginas: 0 }),
    );

    expect(elemento.textContent).toContain('No hay productos que coincidan con los filtros.');
  });

  it('muestra el error en la página, sin notificación, y permite reintentar', async () => {
    const { elemento, boton } = await preparar();
    pendiente().flush(null, { status: 500, statusText: 'Error' });
    await esperar();
    TestBed.tick();

    expect(elemento.querySelector('[role="alert"]')?.textContent).toContain(
      'Ocurrió un error inesperado.',
    );
    expect(TestBed.inject(ServicioNotificaciones).notificaciones()).toEqual([]);

    boton('Reintentar').click();
    pendiente();
  });

  it('el administrador ve crear, editar y eliminar', async () => {
    const { elemento, responder } = await preparar();
    await responder(pendiente());

    expect(elemento.querySelector('a[href="/productos/nuevo"]')).not.toBeNull();
    expect(elemento.querySelector('[aria-label="Editar Teclado"]')).not.toBeNull();
    expect(elemento.querySelector('[aria-label="Eliminar Teclado"]')).not.toBeNull();
  });

  it('el usuario de consulta solo ve el detalle', async () => {
    esAdministrador.set(false);
    const { elemento, responder } = await preparar();
    await responder(pendiente());

    expect(elemento.querySelector('[aria-label="Ver Teclado"]')).not.toBeNull();
    expect(elemento.querySelector('a[href="/productos/nuevo"]')).toBeNull();
    expect(elemento.querySelector('[aria-label="Editar Teclado"]')).toBeNull();
    expect(elemento.querySelector('[aria-label="Eliminar Teclado"]')).toBeNull();
  });

  it('elimina tras confirmar y recarga el listado', async () => {
    const { elemento, responder } = await preparar();
    await responder(pendiente());

    elemento.querySelector<HTMLElement>('[aria-label="Eliminar Teclado"]')!.click();
    await esperar();
    TestBed.tick();
    expect(elemento.querySelector('dialog')?.textContent).toContain(
      'se desactivará, no se borrará',
    );

    elemento.querySelector<HTMLElement>('dialog .boton-peligro')!.click();
    const eliminacion = TestBed.inject(HttpTestingController).expectOne(`${URL}/7`);
    expect(eliminacion.request.method).toBe('DELETE');
    eliminacion.flush(null);
    await esperar();

    const recarga = pendiente();
    pendienteCategorias();
    await responder(recarga);
    expect(elemento.querySelector('dialog')).toBeNull();
  });
});
