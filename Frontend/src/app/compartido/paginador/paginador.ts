import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-paginador',
  templateUrl: './paginador.html',
})
export class Paginador {
  readonly pagina = input.required<number>();
  readonly totalPaginas = input.required<number>();
  readonly totalElementos = input.required<number>();
  readonly cambiar = output<number>();
}
