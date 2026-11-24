import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { ErrorApi } from '../../../nucleo/modelos/error-api';
import { ServicioAutenticacion } from '../../../nucleo/servicios/servicio-autenticacion';
import { ServicioNotificaciones } from '../../../nucleo/servicios/servicio-notificaciones';
import { prepararDialogo } from '../../../pruebas/dialogo-prueba';
import { crearPaginaUsuarios } from '../../../pruebas/usuarios-prueba';
import { ServicioUsuarios } from '../servicios/servicio-usuarios';
import { PaginaUsuarios } from './pagina-usuarios';

describe('PaginaUsuarios', () => {
  const servicio = {
    listar: vi.fn((): Observable<unknown> => of(crearPaginaUsuarios())),
    desactivar: vi.fn((): Observable<unknown> => of(undefined)),
  };

  const preparar = async (pagina?: string) => {
    prepararDialogo();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: ServicioUsuarios, useValue: servicio },
        {
          provide: ServicioAutenticacion,
          useValue: { usuarioActual: signal({ nombreUsuario: 'admin' }) },
        },
      ],
    });
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(PaginaUsuarios);
    fixture.componentRef.setInput('pagina', pagina);
    await fixture.whenStable();
    const elemento: HTMLElement = fixture.nativeElement;
    const pulsar = async (selector: string) => {
      elemento.querySelector<HTMLElement>(selector)!.click();
      await fixture.whenStable();
    };
    return { elemento, pulsar, navegar };
  };

  beforeEach(() => vi.clearAllMocks());

  it('muestra los usuarios en tabla y en tarjetas', async () => {
    const { elemento } = await preparar();

    expect(servicio.listar).toHaveBeenCalledWith(1);
    expect(elemento.querySelectorAll('tbody tr')).toHaveLength(3);
    expect(elemento.querySelectorAll('ul > li')).toHaveLength(3);
    expect(elemento.querySelectorAll('tbody tr')[1].textContent).toContain('ana@ejemplo.com');
    expect(elemento.querySelector('app-paginador')?.textContent).toContain('Página 1 de 2');
  });

  it('solo permite desactivar cuentas activas y ajenas', async () => {
    const { elemento } = await preparar();

    expect(elemento.querySelector('[aria-label="Desactivar ana"]')).not.toBeNull();
    expect(elemento.querySelector('[aria-label="Desactivar admin"]')).toBeNull();
    expect(elemento.querySelector('[aria-label="Desactivar eva"]')).toBeNull();
    expect(elemento.querySelector('[aria-label="Editar eva"]')?.getAttribute('href')).toBe(
      '/usuarios/8/editar',
    );
  });

  it('cambia de página mediante la URL', async () => {
    const { pulsar, navegar } = await preparar();

    await pulsar('app-paginador button:last-child');

    expect(navegar).toHaveBeenCalledWith(['/usuarios'], {
      queryParams: { pagina: 2 },
      replaceUrl: true,
    });
  });

  it.each([
    ['3', 3],
    ['texto', 1],
    ['-1', 1],
    [undefined, 1],
  ])('con pagina=%s pide la página %i', async (texto, pagina) => {
    await preparar(texto);

    expect(servicio.listar).toHaveBeenCalledWith(pagina);
  });

  it('va a la última página si la solicitada no existe', async () => {
    servicio.listar.mockReturnValueOnce(of(crearPaginaUsuarios({ elementos: [], pagina: 9 })));
    const { navegar } = await preparar('9');

    expect(navegar).toHaveBeenCalledWith(
      ['/usuarios'],
      expect.objectContaining({ queryParams: { pagina: 2 } }),
    );
  });

  it('desactiva tras confirmar y recarga', async () => {
    const { elemento, pulsar } = await preparar();

    await pulsar('[aria-label="Desactivar ana"]');
    expect(elemento.querySelector('dialog')?.textContent).toContain('ya no podrá iniciar sesión');
    expect(servicio.desactivar).not.toHaveBeenCalled();

    await pulsar('dialog .boton-peligro');
    expect(servicio.desactivar).toHaveBeenCalledWith(7);
    expect(servicio.listar).toHaveBeenCalledTimes(2);
    expect(elemento.querySelector('dialog')).toBeNull();
    expect(TestBed.inject(ServicioNotificaciones).notificaciones()[0].mensaje).toBe(
      'Usuario desactivado.',
    );
  });

  it('cancelar no desactiva', async () => {
    const { elemento, pulsar } = await preparar();

    await pulsar('[aria-label="Desactivar ana"]');
    await pulsar('dialog .boton-secundario');

    expect(elemento.querySelector('dialog')).toBeNull();
    expect(servicio.desactivar).not.toHaveBeenCalled();
  });

  it('muestra el error de carga y permite reintentar', async () => {
    servicio.listar.mockReturnValueOnce(throwError(() => new ErrorApi(0, 'Sin conexión.')));
    const { elemento, pulsar } = await preparar();
    expect(elemento.querySelector('[role="alert"]')?.textContent).toContain('Sin conexión.');

    await pulsar('[role="alert"] button');

    expect(elemento.querySelectorAll('tbody tr')).toHaveLength(3);
  });
});
