import { NgTemplateOutlet } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { ErrorCarga } from '../../../compartido/error-carga/error-carga';
import { EtiquetaEstado } from '../../../compartido/etiqueta-estado/etiqueta-estado';
import { Paginador } from '../../../compartido/paginador/paginador';
import { ServicioAutenticacion } from '../../../nucleo/servicios/servicio-autenticacion';
import { ConfirmacionDesactivar } from '../confirmacion-desactivar/confirmacion-desactivar';
import { CuentaUsuario } from '../modelos/usuario';
import { ServicioUsuarios } from '../servicios/servicio-usuarios';

@Component({
  imports: [
    ConfirmacionDesactivar,
    ErrorCarga,
    EtiquetaEstado,
    NgTemplateOutlet,
    Paginador,
    RouterLink,
  ],
  selector: 'app-pagina-usuarios',
  templateUrl: './pagina-usuarios.html',
})
export class PaginaUsuarios {
  private readonly servicio = inject(ServicioUsuarios);
  private readonly router = inject(Router);

  readonly pagina = input(1, { transform: leerPagina });

  protected readonly usuarioActual = inject(ServicioAutenticacion).usuarioActual;
  protected readonly porDesactivar = signal<CuentaUsuario | null>(null);

  protected readonly usuarios = rxResource({
    params: () => this.pagina(),
    stream: ({ params }) => this.servicio.listar(params),
  });

  constructor() {
    effect(() => {
      if (!this.usuarios.hasValue()) {
        return;
      }

      const { pagina, totalPaginas } = this.usuarios.value();

      if (totalPaginas > 0 && pagina > totalPaginas) {
        this.irAPagina(totalPaginas);
      }
    });
  }

  protected irAPagina(pagina: number): void {
    void this.router.navigate(['/usuarios'], {
      queryParams: pagina === 1 ? {} : { pagina },
      replaceUrl: true,
    });
  }

  protected alDesactivar(): void {
    this.porDesactivar.set(null);
    this.usuarios.reload();
  }
}

function leerPagina(valor: unknown): number {
  const pagina = Number(valor);
  return Number.isInteger(pagina) && pagina > 0 ? pagina : 1;
}
