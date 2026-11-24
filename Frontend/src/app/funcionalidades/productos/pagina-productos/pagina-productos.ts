import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ServicioAutenticacion } from '../../../nucleo/servicios/servicio-autenticacion';

@Component({
  imports: [RouterLink],
  selector: 'app-pagina-productos',
  templateUrl: './pagina-productos.html',
})
export class PaginaProductos {
  protected readonly esAdministrador = inject(ServicioAutenticacion).esAdministrador;
}
