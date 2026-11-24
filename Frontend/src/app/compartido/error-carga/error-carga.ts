import { Component, computed, input, output } from '@angular/core';
import { ErrorApi } from '../../nucleo/modelos/error-api';

@Component({
  selector: 'app-error-carga',
  templateUrl: './error-carga.html',
})
export class ErrorCarga {
  readonly error = input.required<Error>();
  readonly reintentar = output<void>();

  protected readonly correlacion = computed(() => {
    const error = this.error();
    return error instanceof ErrorApi && error.estado >= 500 ? error.correlacion : undefined;
  });
}
