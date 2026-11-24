import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Observable, of, Subject, throwError } from 'rxjs';
import { ErrorApi } from '../../../nucleo/modelos/error-api';
import { ServicioAutenticacion } from '../../../nucleo/servicios/servicio-autenticacion';
import { PaginaInicioSesion } from './pagina-inicio-sesion';

describe('PaginaInicioSesion', () => {
  const iniciarSesion = vi.fn<() => Observable<unknown>>();

  const preparar = (retorno?: string) => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: ServicioAutenticacion, useValue: { iniciarSesion } },
      ],
    });
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(PaginaInicioSesion);
    fixture.componentRef.setInput('retorno', retorno);
    const elemento: HTMLElement = fixture.nativeElement;

    const enviar = async (usuario: string, contrasena: string) => {
      await fixture.whenStable();
      [usuario, contrasena].forEach((valor, indice) => {
        const campo = elemento.querySelectorAll('input')[indice];
        campo.value = valor;
        campo.dispatchEvent(new Event('input'));
        campo.dispatchEvent(new Event('blur'));
      });
      elemento.querySelector('form')?.dispatchEvent(new Event('submit'));
      await fixture.whenStable();
    };

    return { elemento, enviar, navegar };
  };

  beforeEach(() => iniciarSesion.mockReset());

  it('vuelve a la ruta solicitada tras iniciar sesión', async () => {
    iniciarSesion.mockReturnValue(of({}));
    const { enviar, navegar } = preparar('/productos/nuevo');

    await enviar('admin', 'clave');

    expect(iniciarSesion).toHaveBeenCalledWith({ nombreUsuario: 'admin', contrasena: 'clave' });
    expect(navegar).toHaveBeenCalledWith('/productos/nuevo');
  });

  it('enlaza con la creación de cuentas', async () => {
    const { elemento } = preparar();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(elemento.querySelector('a')?.getAttribute('href')).toBe('/crear-cuenta');
  });

  it('avisa en el campo si falta un dato y no llama a la API', async () => {
    const { elemento, enviar } = preparar();

    await enviar('admin', '');

    expect(elemento.querySelector('#usuario-error')).toBeNull();
    expect(elemento.querySelector('#contrasena-error')?.textContent).toBe(
      'Este campo es obligatorio.',
    );
    expect(iniciarSesion).not.toHaveBeenCalled();
  });

  it('bloquea el botón mientras espera y no envía dos veces', async () => {
    iniciarSesion.mockReturnValue(new Subject());
    const { elemento, enviar } = preparar();

    await enviar('admin', 'clave');
    elemento.querySelector('form')?.dispatchEvent(new Event('submit'));

    expect(elemento.querySelector('button')?.disabled).toBe(true);
    expect(elemento.querySelector('button')?.textContent?.trim()).toBe('Ingresando…');
    expect(iniciarSesion).toHaveBeenCalledOnce();
  });

  it.each([
    [401, 'El usuario o la contraseña no son correctos.'],
    [429, 'Demasiadas peticiones. Vuelve a intentarlo en 30 segundos.'],
  ])('muestra el error %i y limpia solo la contraseña', async (estado, mensaje) => {
    iniciarSesion.mockReturnValue(throwError(() => new ErrorApi(estado, mensaje)));
    const { elemento, enviar, navegar } = preparar();

    await enviar('admin', 'incorrecta');

    expect(elemento.querySelector('[role="alert"]')?.textContent).toContain(mensaje);
    expect(elemento.querySelector<HTMLInputElement>('#contrasena')?.value).toBe('');
    expect(elemento.querySelector<HTMLInputElement>('#usuario')?.value).toBe('admin');
    expect(elemento.querySelector('#contrasena-error')).toBeNull();
    expect(elemento.querySelector('button')?.disabled).toBe(false);
    expect(navegar).not.toHaveBeenCalled();
  });
});
