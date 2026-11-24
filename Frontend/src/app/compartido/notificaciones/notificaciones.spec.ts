import { TestBed } from '@angular/core/testing';
import { ServicioNotificaciones } from '../../nucleo/servicios/servicio-notificaciones';
import { Notificaciones } from './notificaciones';

describe('Notificaciones', () => {
  it('anuncia los errores como alerta y permite cerrarlos', async () => {
    const fixture = TestBed.createComponent(Notificaciones);
    const elemento: HTMLElement = fixture.nativeElement;

    TestBed.inject(ServicioNotificaciones).error('Algo falló.', 'abc-123');
    await fixture.whenStable();

    const alerta = elemento.querySelector('[role="alert"]');
    expect(alerta?.textContent).toContain('Algo falló.');
    expect(alerta?.textContent).toContain('abc-123');

    elemento.querySelector<HTMLButtonElement>('[aria-label="Cerrar notificación"]')?.click();
    await fixture.whenStable();

    expect(elemento.querySelector('[role="alert"]')).toBeNull();
  });
});
