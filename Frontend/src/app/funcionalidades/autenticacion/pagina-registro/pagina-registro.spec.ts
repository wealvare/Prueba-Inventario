import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Observable, of, Subject, throwError } from 'rxjs';
import { ErrorApi } from '../../../nucleo/modelos/error-api';
import { ServicioAutenticacion } from '../../../nucleo/servicios/servicio-autenticacion';
import { ServicioNotificaciones } from '../../../nucleo/servicios/servicio-notificaciones';
import { PaginaRegistro } from './pagina-registro';

describe('PaginaRegistro', () => {
  const registrar = vi.fn<() => Observable<unknown>>();

  const preparar = () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: ServicioAutenticacion, useValue: { registrar } }],
    });
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(PaginaRegistro);
    const elemento: HTMLElement = fixture.nativeElement;

    const escribir = async (id: string, valor: string) => {
      const campo = elemento.querySelector<HTMLInputElement>(`#${id}`)!;
      campo.value = valor;
      campo.dispatchEvent(new Event('input'));
      campo.dispatchEvent(new Event('blur'));
      await fixture.whenStable();
    };
    const enviar = async (cambios: Record<string, string> = {}) => {
      await fixture.whenStable();
      const valores = {
        nombreUsuario: ' ana ',
        correo: 'ana@ejemplo.com',
        contrasena: 'Clave123',
        confirmacion: 'Clave123',
        ...cambios,
      };
      for (const [id, valor] of Object.entries(valores)) {
        await escribir(id, valor);
      }
      elemento.querySelector('form')!.dispatchEvent(new Event('submit'));
      await fixture.whenStable();
    };
    const error = (id: string) => elemento.querySelector(`#${id}-error`)?.textContent;
    const alerta = () => elemento.querySelector('[role="alert"]')?.textContent;

    return { elemento, enviar, escribir, error, alerta, navegar };
  };

  beforeEach(() => registrar.mockReset());

  it('registra sin enviar rol ni confirmación y vuelve al inicio de sesión con aviso', async () => {
    registrar.mockReturnValue(of(undefined));
    const { enviar, navegar } = preparar();

    await enviar();

    expect(registrar).toHaveBeenCalledWith({
      nombreUsuario: 'ana',
      correo: 'ana@ejemplo.com',
      contrasena: 'Clave123',
    });
    expect(navegar).toHaveBeenCalledWith('/iniciar-sesion');
    expect(TestBed.inject(ServicioNotificaciones).notificaciones()[0]).toMatchObject({
      tipo: 'exito',
      mensaje: 'Cuenta creada. Ya puedes iniciar sesión.',
    });
  });

  it.each([
    ['nombreUsuario', 'a b', 'Usa al menos 3 caracteres'],
    ['correo', 'ana', 'El correo no tiene un formato válido.'],
    ['contrasena', 'clave123', 'con mayúscula, minúscula y número'],
    ['confirmacion', 'Clave124', 'Las contraseñas no coinciden.'],
    ['confirmacion', '', 'Este campo es obligatorio.'],
  ])('avisa si %s vale "%s" y no llama a la API', async (id, valor, mensaje) => {
    const { elemento, enviar, error } = preparar();

    await enviar({ [id]: valor });

    expect(error(id)).toContain(mensaje);
    expect(elemento.querySelector(`#${id}`)?.getAttribute('aria-invalid')).toBe('true');
    expect(registrar).not.toHaveBeenCalled();
  });

  it.each([
    [409, "Ya existe un usuario con el nombre 'ana'."],
    [429, 'Demasiadas peticiones. Vuelve a intentarlo en 30 segundos.'],
  ])('muestra el error %i, conserva los datos y lo limpia al corregir', async (estado, mensaje) => {
    registrar.mockReturnValue(throwError(() => new ErrorApi(estado, mensaje)));
    const { elemento, enviar, escribir, alerta, navegar } = preparar();

    await enviar();

    expect(alerta()).toContain(mensaje);
    expect(elemento.querySelector<HTMLInputElement>('#correo')?.value).toBe('ana@ejemplo.com');
    expect(elemento.querySelector('button')?.disabled).toBe(false);
    expect(navegar).not.toHaveBeenCalled();

    await escribir('nombreUsuario', 'otra');
    expect(alerta()).toBeUndefined();
  });

  it('muestra los errores 400 junto a cada campo', async () => {
    registrar.mockReturnValue(
      throwError(() => new ErrorApi(400, 'Revisa.', 'c-1', { correo: ['Correo rechazado.'] })),
    );
    const { enviar, error } = preparar();

    await enviar();

    expect(error('correo')).toBe('Correo rechazado.');
  });

  it('bloquea el botón mientras espera y no envía dos veces', async () => {
    registrar.mockReturnValue(new Subject());
    const { elemento, enviar } = preparar();

    await enviar();
    elemento.querySelector('form')!.dispatchEvent(new Event('submit'));

    expect(elemento.querySelector('button')?.disabled).toBe(true);
    expect(registrar).toHaveBeenCalledOnce();
  });
});
