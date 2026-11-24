import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { entorno } from '../../../../entornos/entorno';
import { ResultadoPaginado } from '../../../compartido/modelos/resultado-paginado';
import { mostrarEnPantalla } from '../../../nucleo/interceptores/errores-en-pantalla';
import {
  CuentaUsuario,
  SolicitudActualizarUsuario,
  SolicitudCrearUsuario,
} from '../modelos/usuario';

const URL = `${entorno.urlApi}/usuarios`;

@Injectable({ providedIn: 'root' })
export class ServicioUsuarios {
  private readonly http = inject(HttpClient);

  listar(pagina: number): Observable<ResultadoPaginado<CuentaUsuario>> {
    return this.http.get<ResultadoPaginado<CuentaUsuario>>(URL, {
      params: { pagina },
      context: mostrarEnPantalla(),
    });
  }

  obtener(id: number): Observable<CuentaUsuario> {
    return this.http.get<CuentaUsuario>(`${URL}/${id}`, { context: mostrarEnPantalla() });
  }

  crear(solicitud: SolicitudCrearUsuario): Observable<CuentaUsuario> {
    return this.http.post<CuentaUsuario>(URL, solicitud, { context: mostrarEnPantalla(400, 409) });
  }

  actualizar(id: number, solicitud: SolicitudActualizarUsuario): Observable<CuentaUsuario> {
    return this.http.put<CuentaUsuario>(`${URL}/${id}`, solicitud, {
      context: mostrarEnPantalla(400, 409),
    });
  }

  desactivar(id: number): Observable<void> {
    return this.http.delete<void>(`${URL}/${id}`);
  }
}
