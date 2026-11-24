import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ServicioAutenticacion } from '../servicios/servicio-autenticacion';
import { DisenoPrincipal } from './diseno-principal';

describe('DisenoPrincipal', () => {
  it.each([
    [true, ['Productos', 'Usuarios']],
    [false, ['Productos']],
  ])('con administrador=%s muestra en el menú %s', async (administrador, esperado) => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: ServicioAutenticacion,
          useValue: { usuarioActual: signal(null), esAdministrador: signal(administrador) },
        },
      ],
    });
    const fixture = TestBed.createComponent(DisenoPrincipal);
    await fixture.whenStable();

    const enlaces = [...(fixture.nativeElement as HTMLElement).querySelectorAll('nav a')];
    expect(enlaces.map((enlace) => enlace.textContent?.trim())).toEqual(esperado);
  });
});
