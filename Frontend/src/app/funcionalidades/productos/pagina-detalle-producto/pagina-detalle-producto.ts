import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, input, numberAttribute, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { EtiquetaEstado } from '../../../compartido/etiqueta-estado/etiqueta-estado';
import { ConfirmacionEliminar } from '../confirmacion-eliminar/confirmacion-eliminar';
import { ServicioProductos } from '../servicios/servicio-productos';

@Component({
  imports: [ConfirmacionEliminar, DatePipe, DecimalPipe, EtiquetaEstado, RouterLink],
  selector: 'app-pagina-detalle-producto',
  templateUrl: './pagina-detalle-producto.html',
})
export class PaginaDetalleProducto {
  private readonly servicio = inject(ServicioProductos);

  readonly id = input.required({ transform: numberAttribute });

  protected readonly confirmando = signal(false);
  protected readonly producto = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.servicio.obtener(params),
  });

  protected alEliminar(): void {
    this.confirmando.set(false);
    this.producto.reload();
  }
}
