import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { entorno } from '../../../../entornos/entorno';
import { mostrarEnPantalla } from '../../../nucleo/interceptores/errores-en-pantalla';
import { FiltroProductos, ProductoResumen, ResultadoPaginado } from '../modelos/producto';

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
}
