import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { prepararDialogo } from '../../pruebas/dialogo-prueba';
import { Dialogo } from './dialogo';

@Component({
  imports: [Dialogo],
  template: `
    @if (abierto()) {
      <app-dialogo titulo="Título" (cerrado)="abierto.set(false)">
        <button acciones id="primero">Primero</button>
        <button acciones id="ultimo">Último</button>
      </app-dialogo>
    }
  `,
})
class Anfitrion {
  readonly abierto = signal(true);
}

describe('Dialogo', () => {
  const preparar = async () => {
    prepararDialogo();
    const fixture = TestBed.createComponent(Anfitrion);
    document.body.append(fixture.nativeElement);
    await fixture.whenStable();
    const elemento: HTMLElement = fixture.nativeElement;
    const tecla = (shiftKey: boolean) =>
      document.activeElement?.dispatchEvent(
        new KeyboardEvent('keydown', { key: 'Tab', shiftKey, bubbles: true, cancelable: true }),
      );
    return { fixture, elemento, tecla, dialogo: elemento.querySelector('dialog')! };
  };

  it('se abre como modal y con título accesible', async () => {
    const { dialogo, elemento } = await preparar();

    expect(dialogo.open).toBe(true);
    expect(elemento.querySelector(`#${dialogo.getAttribute('aria-labelledby')}`)?.textContent).toBe(
      'Título',
    );
  });

  it('mantiene el foco dentro con Tab y Mayús+Tab', async () => {
    const { elemento, tecla } = await preparar();
    elemento.querySelector<HTMLElement>('#ultimo')!.focus();

    tecla(false);
    expect(document.activeElement?.id).toBe('primero');
    tecla(true);
    expect(document.activeElement?.id).toBe('ultimo');
  });

  it('devuelve el foco al elemento que lo abrió', async () => {
    const boton = document.createElement('button');
    document.body.append(boton);
    boton.focus();
    const { fixture, elemento, dialogo } = await preparar();
    elemento.querySelector<HTMLElement>('#primero')!.focus();

    dialogo.dispatchEvent(new Event('cancel', { cancelable: true }));
    await fixture.whenStable();
    await new Promise((resolver) => setTimeout(resolver));

    expect(document.activeElement).toBe(boton);
    boton.remove();
  });

  it('se cierra con Escape', async () => {
    const { fixture, elemento, dialogo } = await preparar();

    dialogo.dispatchEvent(new Event('cancel', { cancelable: true }));
    await fixture.whenStable();

    expect(elemento.querySelector('dialog')).toBeNull();
  });
});
