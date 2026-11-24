import {
  afterNextRender,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

const ENFOCABLES = 'button:not(:disabled), a[href], input:not(:disabled)';

@Component({
  selector: 'app-dialogo',
  templateUrl: './dialogo.html',
})
export class Dialogo {
  readonly titulo = input.required<string>();
  readonly cerrado = output<void>();

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  constructor() {
    const focoAnterior = document.activeElement;

    afterNextRender(() => this.dialogo().nativeElement.showModal());
    inject(DestroyRef).onDestroy(() => {
      this.dialogo().nativeElement.close();
      setTimeout(() => (focoAnterior as HTMLElement | null)?.focus());
    });
  }

  protected cancelar(evento: Event): void {
    evento.preventDefault();
    this.cerrado.emit();
  }

  protected atraparFoco(evento: Event, haciaAtras: boolean): void {
    const elementos = [...this.dialogo().nativeElement.querySelectorAll<HTMLElement>(ENFOCABLES)];
    const primero = elementos.at(0);
    const ultimo = elementos.at(-1);

    if (haciaAtras && document.activeElement === primero) {
      evento.preventDefault();
      ultimo?.focus();
    } else if (!haciaAtras && document.activeElement === ultimo) {
      evento.preventDefault();
      primero?.focus();
    }
  }
}
