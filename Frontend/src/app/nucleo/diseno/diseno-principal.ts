import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ServicioAutenticacion } from '../servicios/servicio-autenticacion';

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  selector: 'app-diseno-principal',
  templateUrl: './diseno-principal.html',
})
export class DisenoPrincipal {
  private readonly autenticacion = inject(ServicioAutenticacion);

  protected readonly usuario = this.autenticacion.usuarioActual;
  protected readonly menuAbierto = signal(false);

  protected alternarMenu(): void {
    this.menuAbierto.update((abierto) => !abierto);
  }

  protected cerrarMenu(): void {
    this.menuAbierto.set(false);
  }

  protected salir(): void {
    this.autenticacion.cerrarSesion();
  }
}
