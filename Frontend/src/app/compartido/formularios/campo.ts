import { Component, input } from '@angular/core';

@Component({
  selector: 'app-campo',
  templateUrl: './campo.html',
})
export class Campo {
  readonly idControl = input.required<string>();
  readonly etiqueta = input.required<string>();
  readonly error = input<string | null>(null);
}
