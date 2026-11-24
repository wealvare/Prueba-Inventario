import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
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

  it('avisa si faltan datos y no llama a la API', async () => {
    const { elemento, enviar } = preparar();

    await enviar('admin', '');

    expect(elemento.querySelector('[role="alert"]')).not.toBeNull();
    expect(iniciarSesion).not.toHaveBeenCalled();
  });

  it('si el inicio de sesión falla, limpia la contraseña sin avisar que faltan datos', async () => {
    iniciarSesion.mockReturnValue(throwError(() => new Error()));
    const { elemento, enviar, navegar } = preparar();

    await enviar('admin', 'incorrecta');

    expect(elemento.querySelector<HTMLInputElement>('input[type="password"]')?.value).toBe('');
    expect(elemento.querySelector('[role="alert"]')).toBeNull();
    expect(navegar).not.toHaveBeenCalled();
  });
});
