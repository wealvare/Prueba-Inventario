import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { ErrorApi } from '../../../nucleo/modelos/error-api';
import { USUARIO } from '../../../pruebas/usuarios-prueba';
import { ServicioUsuarios } from '../servicios/servicio-usuarios';
import { PaginaFormularioUsuario } from './pagina-formulario-usuario';

describe('PaginaFormularioUsuario', () => {
  const servicio = {
    obtener: vi.fn((): Observable<unknown> => of(USUARIO)),
    crear: vi.fn((): Observable<unknown> => of(USUARIO)),
    actualizar: vi.fn((): Observable<unknown> => of(USUARIO)),
  };

  const preparar = async (id?: string) => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: ServicioUsuarios, useValue: servicio }],
    });
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(PaginaFormularioUsuario);
    fixture.componentRef.setInput('id', id);
    await fixture.whenStable();
    const elemento: HTMLElement = fixture.nativeElement;

    const escribir = async (campo: string, valor: string) => {
      const control = elemento.querySelector<HTMLInputElement>(`#${campo}`)!;
      control.value = valor;
      control.dispatchEvent(new Event(control.tagName === 'SELECT' ? 'change' : 'input'));
      control.dispatchEvent(new Event('blur'));
      await fixture.whenStable();
    };
    const guardar = async () => {
      elemento.querySelector('form')!.dispatchEvent(new Event('submit'));
      await fixture.whenStable();
    };
    const valor = (campo: string) => elemento.querySelector<HTMLInputElement>(`#${campo}`)?.value;
    const error = (campo: string) => elemento.querySelector(`#${campo}-error`)?.textContent;
    const alerta = () => elemento.querySelector('[role="alert"]')?.textContent;

    return { elemento, navegar, escribir, guardar, valor, error, alerta };
  };

  beforeEach(() => vi.clearAllMocks());

  it('al crear envía los datos con el rol elegido y vuelve al listado', async () => {
    const pagina = await preparar();
    expect(pagina.elemento.querySelector('input[type="checkbox"]')).toBeNull();
    await pagina.escribir('nombreUsuario', ' eva ');
    await pagina.escribir('correo', 'eva@ejemplo.com');
    await pagina.escribir('contrasena', 'Clave123');
    await pagina.escribir('rol', 'Administrador');

    await pagina.guardar();

    expect(servicio.crear).toHaveBeenCalledWith({
      nombreUsuario: 'eva',
      correo: 'eva@ejemplo.com',
      contrasena: 'Clave123',
      rol: 'Administrador',
    });
    expect(pagina.navegar).toHaveBeenCalledWith('/usuarios');
  });

  it('al crear exige todos los campos', async () => {
    const pagina = await preparar();

    await pagina.guardar();

    for (const campo of ['nombreUsuario', 'correo', 'contrasena']) {
      expect(pagina.error(campo)).toBe('Este campo es obligatorio.');
    }
    expect(servicio.crear).not.toHaveBeenCalled();
  });

  it('al editar carga los datos y sin contraseña envía null', async () => {
    const pagina = await preparar('7');
    expect([
      pagina.valor('nombreUsuario'),
      pagina.valor('rol'),
      pagina.valor('contrasena'),
    ]).toEqual(['ana', 'Consulta', '']);

    pagina.elemento.querySelector<HTMLElement>('input[type="checkbox"]')!.click();
    await pagina.guardar();

    expect(servicio.actualizar).toHaveBeenCalledWith(7, {
      nombreUsuario: 'ana',
      correo: 'ana@ejemplo.com',
      rol: 'Consulta',
      activo: false,
      contrasena: null,
    });
  });

  it('al editar valida y envía la contraseña si se escribe', async () => {
    const pagina = await preparar('7');

    await pagina.escribir('contrasena', 'corta');
    await pagina.guardar();
    expect(pagina.error('contrasena')).toContain('entre 8 y 100 caracteres');
    expect(servicio.actualizar).not.toHaveBeenCalled();

    await pagina.escribir('contrasena', 'Nueva456');
    await pagina.guardar();
    expect(servicio.actualizar).toHaveBeenCalledWith(
      7,
      expect.objectContaining({ contrasena: 'Nueva456' }),
    );
  });

  it('muestra un 409 y conserva los datos', async () => {
    const mensaje = 'El sistema debe conservar al menos un administrador activo.';
    servicio.actualizar.mockReturnValueOnce(throwError(() => new ErrorApi(409, mensaje)));
    const pagina = await preparar('7');
    await pagina.escribir('nombreUsuario', 'cambio');

    await pagina.guardar();

    expect(pagina.alerta()).toContain(mensaje);
    expect(pagina.valor('nombreUsuario')).toBe('cambio');
    expect(pagina.navegar).not.toHaveBeenCalled();
  });

  it('muestra los errores 400 junto a cada campo y los limpia al corregir', async () => {
    servicio.actualizar.mockReturnValueOnce(
      throwError(() => new ErrorApi(400, 'Revisa.', 'c-1', { correo: ['Correo rechazado.'] })),
    );
    const pagina = await preparar('7');

    await pagina.guardar();
    expect(pagina.error('correo')).toBe('Correo rechazado.');

    await pagina.escribir('correo', 'otro@ejemplo.com');
    expect(pagina.error('correo')).toBeUndefined();
  });

  it('si no puede cargar el usuario muestra el error en lugar del formulario', async () => {
    servicio.obtener.mockReturnValueOnce(
      throwError(() => new ErrorApi(404, 'No existe un usuario con el id 7.')),
    );
    const pagina = await preparar('7');

    expect(pagina.alerta()).toContain('No existe un usuario con el id 7.');
    expect(pagina.elemento.querySelector('form')).toBeNull();
  });
});
