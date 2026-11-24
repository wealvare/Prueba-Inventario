import { Component, inject, input, output, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { Dialogo } from '../../../compartido/dialogo/dialogo';
import { ServicioNotificaciones } from '../../../nucleo/servicios/servicio-notificaciones';
import { ServicioUsuarios } from '../servicios/servicio-usuarios';

@Component({
  imports: [Dialogo],
  selector: 'app-confirmacion-desactivar',
  templateUrl: './confirmacion-desactivar.html',
})
export class ConfirmacionDesactivar {
  private readonly servicio = inject(ServicioUsuarios);
  private readonly notificaciones = inject(ServicioNotificaciones);

  readonly id = input.required<number>();
  readonly nombre = input.required<string>();
  readonly desactivado = output<void>();
  readonly cancelado = output<void>();

  protected readonly desactivando = signal(false);

  protected cancelar(): void {
    if (!this.desactivando()) {
      this.cancelado.emit();
    }
  }

  protected desactivar(): void {
    if (this.desactivando()) {
      return;
    }

    this.desactivando.set(true);
    this.servicio
      .desactivar(this.id())
      .pipe(finalize(() => this.desactivando.set(false)))
      .subscribe({
        next: () => {
          this.notificaciones.exito('Usuario desactivado.');
          this.desactivado.emit();
        },
        error: () => this.cancelado.emit(),
      });
  }
}
