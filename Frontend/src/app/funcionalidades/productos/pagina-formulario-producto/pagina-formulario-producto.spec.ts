import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { ErrorApi } from '../../../nucleo/modelos/error-api';
import { PRODUCTO } from '../../../pruebas/productos-prueba';
import { ServicioProductos } from '../servicios/servicio-productos';
import { PaginaFormularioProducto } from './pagina-formulario-producto';

describe('PaginaFormularioProducto', () => {
  const servicio = {
    obtener: vi.fn((): Observable<unknown> => of(PRODUCTO)),
    listarCategorias: vi.fn((): Observable<unknown> => of(['Audio', 'Redes'])),
    crear: vi.fn((): Observable<unknown> => of(PRODUCTO)),
    actualizar: vi.fn((): Observable<unknown> => of(PRODUCTO)),
  };

  const preparar = async (id?: string) => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: ServicioProductos, useValue: servicio }],
    });
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(PaginaFormularioProducto);
    fixture.componentRef.setInput('id', id);
    await fixture.whenStable();
    const elemento: HTMLElement = fixture.nativeElement;

    const escribir = async (selector: string, valor: string) => {
      const campo = elemento.querySelector<HTMLInputElement>(selector)!;
      campo.value = valor;
      campo.dispatchEvent(new Event('input'));
      campo.dispatchEvent(new Event('blur'));
      await fixture.whenStable();
    };
    const pulsar = async (selector: string) => {
      elemento.querySelector<HTMLElement>(selector)!.click();
      await fixture.whenStable();
    };
    const guardar = async () => {
      elemento.querySelector('form')!.dispatchEvent(new Event('submit'));
      await fixture.whenStable();
    };
    const error = (campo: string) => elemento.querySelector(`#${campo}-error`)?.textContent?.trim();
    const alerta = () => elemento.querySelector('[role="alert"]');

    return { elemento, navegar, escribir, pulsar, guardar, error, alerta };
  };

  const completar = async ({ escribir }: Awaited<ReturnType<typeof preparar>>) => {
    await escribir('#codigo', 'NUE-1');
    await escribir('#nombre', 'Nuevo');
    await escribir('#categoria', 'Otra');
  };

  beforeEach(() => vi.clearAllMocks());

  it('al crear envía los datos sin el campo activo y abre el detalle', async () => {
    const pagina = await preparar();
    await completar(pagina);
    await pagina.pulsar('#agregar-lote');
    await pagina.escribir('#numeroLote-0', 'L-1');
    await pagina.escribir('#fechaIngreso-0', '2026-03-01');
    await pagina.escribir('#precio-0', '10.5');
    await pagina.escribir('#cantidad-0', '3');

    await pagina.guardar();

    expect(servicio.crear).toHaveBeenCalledWith({
      codigo: 'NUE-1',
      nombre: 'Nuevo',
      descripcion: '',
      categoria: 'Otra',
      lotes: [
        { id: null, numeroLote: 'L-1', fechaIngreso: '2026-03-01', precio: 10.5, cantidad: 3 },
      ],
    });
    expect(pagina.navegar).toHaveBeenCalledWith(['/productos', 7]);
  });

  it('sugiere las categorías existentes', async () => {
    const { elemento } = await preparar();

    const opciones = [...elemento.querySelectorAll<HTMLOptionElement>('datalist option')];
    expect(opciones.map(({ value }) => value)).toEqual(['Audio', 'Redes']);
    expect(elemento.querySelector('#categoria')?.getAttribute('list')).toBe(
      elemento.querySelector('datalist')?.id,
    );
  });

  it('permite continuar si fallan las sugerencias', async () => {
    servicio.listarCategorias.mockReturnValueOnce(throwError(() => new ErrorApi(0, 'Sin red.')));
    const pagina = await preparar();
    await completar(pagina);

    await pagina.guardar();

    expect(pagina.elemento.textContent).toContain('No se pudieron cargar las sugerencias.');
    expect(servicio.crear).toHaveBeenCalledOnce();
  });

  it('al editar carga el producto y siempre envía activo', async () => {
    const pagina = await preparar('7');
    expect(pagina.elemento.querySelector<HTMLInputElement>('#numeroLote-1')?.value).toBe('L-2');

    await pagina.pulsar('input[type="checkbox"]');
    await pagina.guardar();

    expect(servicio.actualizar).toHaveBeenCalledWith(
      7,
      expect.objectContaining({ activo: false, lotes: PRODUCTO.lotes }),
    );
  });

  it('quita el lote indicado', async () => {
    const pagina = await preparar('7');

    await pagina.pulsar('[aria-label="Quitar lote 1"]');
    await pagina.guardar();

    expect(servicio.actualizar).toHaveBeenCalledWith(
      7,
      expect.objectContaining({ lotes: [PRODUCTO.lotes[1]] }),
    );
  });

  it('no envía un formulario inválido y muestra los errores', async () => {
    const pagina = await preparar();

    await pagina.guardar();

    expect(servicio.crear).not.toHaveBeenCalled();
    expect(pagina.error('codigo')).toBe('Este campo es obligatorio.');
    expect(pagina.elemento.querySelector('#codigo')?.getAttribute('aria-invalid')).toBe('true');
  });

  it('marca los lotes repetidos y no envía', async () => {
    const pagina = await preparar('7');

    await pagina.escribir('#numeroLote-1', 'l-1');
    await pagina.guardar();

    expect(pagina.error('numeroLote-0')).toBe('El número de lote está repetido.');
    expect(pagina.error('numeroLote-1')).toBe('El número de lote está repetido.');
    expect(servicio.actualizar).not.toHaveBeenCalled();
  });

  it('muestra los errores 400 junto a cada campo y los limpia al corregir', async () => {
    servicio.actualizar.mockReturnValueOnce(
      throwError(
        () =>
          new ErrorApi(400, 'Revisa los datos enviados.', 'c-1', {
            codigo: ['Código rechazado.'],
            'lotes[1].precio': ['Precio rechazado.'],
          }),
      ),
    );
    const pagina = await preparar('7');

    await pagina.guardar();
    expect(pagina.error('codigo')).toBe('Código rechazado.');
    expect(pagina.error('precio-1')).toBe('Precio rechazado.');
    expect(pagina.alerta()?.textContent).toContain('Revisa los datos enviados.');

    await pagina.escribir('#codigo', 'OTRO');
    expect(pagina.error('codigo')).toBeUndefined();
    expect(pagina.alerta()).toBeNull();
  });

  it('muestra un 409 como mensaje y permite recargar al editar', async () => {
    const mensaje = "Ya existe un producto con el código 'TEC-1'.";
    servicio.actualizar.mockReturnValueOnce(throwError(() => new ErrorApi(409, mensaje)));
    const pagina = await preparar('7');
    await pagina.escribir('#nombre', 'Cambio');

    await pagina.guardar();
    expect(pagina.alerta()?.textContent).toContain(mensaje);
    expect(pagina.elemento.querySelector<HTMLInputElement>('#nombre')?.value).toBe('Cambio');

    await pagina.pulsar('[role="alert"] button');
    expect(pagina.elemento.querySelector<HTMLInputElement>('#nombre')?.value).toBe('Teclado');
    expect(pagina.alerta()).toBeNull();
  });

  it('si no puede cargar el producto muestra el error en lugar del formulario', async () => {
    servicio.obtener.mockReturnValueOnce(
      throwError(() => new ErrorApi(404, 'No existe un producto con el id 7.')),
    );
    const pagina = await preparar('7');

    expect(pagina.alerta()?.textContent).toContain('No existe un producto con el id 7.');
    expect(pagina.elemento.querySelector('form')).toBeNull();
  });
});
