import { Component, inject, input, output, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { Dialogo } from '../../../compartido/dialogo/dialogo';
import { ServicioNotificaciones } from '../../../nucleo/servicios/servicio-notificaciones';
import { ServicioProductos } from '../servicios/servicio-productos';

@Component({
  imports: [Dialogo],
  selector: 'app-confirmacion-eliminar',
  templateUrl: './confirmacion-eliminar.html',
})
export class ConfirmacionEliminar {
  private readonly servicio = inject(ServicioProductos);
  private readonly notificaciones = inject(ServicioNotificaciones);

  readonly id = input.required<number>();
  readonly nombre = input.required<string>();
  readonly eliminado = output<void>();
  readonly cancelado = output<void>();

  protected readonly eliminando = signal(false);

  protected cancelar(): void {
    if (!this.eliminando()) {
      this.cancelado.emit();
    }
  }

  protected eliminar(): void {
    if (this.eliminando()) {
      return;
    }

    this.eliminando.set(true);
    this.servicio
      .eliminar(this.id())
      .pipe(finalize(() => this.eliminando.set(false)))
      .subscribe({
        next: () => {
          this.notificaciones.exito('Producto desactivado.');
          this.eliminado.emit();
        },
        error: () => this.cancelado.emit(),
      });
  }
}
