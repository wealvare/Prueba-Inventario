import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { entorno } from '../../../../entornos/entorno';
import { mostrarEnPantalla } from '../../../nucleo/interceptores/errores-en-pantalla';
import {
  FiltroProductos,
  Producto,
  ProductoResumen,
  ResultadoPaginado,
  SolicitudActualizarProducto,
  SolicitudCrearProducto,
} from '../modelos/producto';

const URL = `${entorno.urlApi}/productos`;

@Injectable({ providedIn: 'root' })
export class ServicioProductos {
  private readonly http = inject(HttpClient);

  listar(filtro: FiltroProductos): Observable<ResultadoPaginado<ProductoResumen>> {
    const valores = Object.entries(filtro).filter(([, valor]) => valor !== null && valor !== '');
    const params = new HttpParams({ fromObject: Object.fromEntries(valores) });

    return this.http.get<ResultadoPaginado<ProductoResumen>>(URL, {
      params,
      context: mostrarEnPantalla(),
    });
  }

  obtener(id: number): Observable<Producto> {
    return this.http.get<Producto>(`${URL}/${id}`, { context: mostrarEnPantalla() });
  }

  crear(solicitud: SolicitudCrearProducto): Observable<Producto> {
    return this.http.post<Producto>(URL, solicitud, { context: mostrarEnPantalla(400, 409) });
  }

  actualizar(id: number, solicitud: SolicitudActualizarProducto): Observable<Producto> {
    return this.http.put<Producto>(`${URL}/${id}`, solicitud, {
      context: mostrarEnPantalla(400, 409),
    });
  }
}
