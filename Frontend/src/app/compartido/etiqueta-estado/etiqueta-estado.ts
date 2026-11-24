import { Component, input } from '@angular/core';

@Component({
  selector: 'app-etiqueta-estado',
  template: `
    <span
      class="inline-block rounded-full px-2.5 py-1 text-xs font-medium"
      [class]="activo() ? 'bg-acento-suave text-acento-fuerte' : 'bg-neutral-100 text-neutral-600'"
    >
      {{ activo() ? 'Activo' : 'Inactivo' }}
    </span>
  `,
})
export class EtiquetaEstado {
  readonly activo = input.required<boolean>();
}
