import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, input, numberAttribute } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { EtiquetaEstado } from '../../../compartido/etiqueta-estado/etiqueta-estado';
import { ServicioProductos } from '../servicios/servicio-productos';

@Component({
  imports: [DatePipe, DecimalPipe, EtiquetaEstado, RouterLink],
  selector: 'app-pagina-detalle-producto',
  templateUrl: './pagina-detalle-producto.html',
})
export class PaginaDetalleProducto {
  private readonly servicio = inject(ServicioProductos);

  readonly id = input.required({ transform: numberAttribute });

  protected readonly producto = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.servicio.obtener(params),
  });
}
