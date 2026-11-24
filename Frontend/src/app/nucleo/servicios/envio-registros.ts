import { HttpBackend, HttpClient } from '@angular/common/http';
import { DOCUMENT, inject, Injectable, Injector } from '@angular/core';
import { entorno } from '../../../entornos/entorno';
import { ocultarDatosSensibles } from '../errores/ocultar-datos-sensibles';
import { DatosRegistro, NivelRegistro, RegistroCliente } from '../modelos/registro-cliente';

export const URL_REGISTROS = `${entorno.urlApi}/registros-cliente`;

const VENTANA_MS = 60_000;
const MAXIMO_POR_VENTANA = 5;
const LONGITUD_MENSAJE = 500;
const LONGITUD_RUTA = 200;
const FORMATO_TIPO = /^[A-Za-z][A-Za-z0-9_]{0,59}$/;
const FORMATO_CORRELACION = /^[A-Za-z0-9-]{1,64}$/;

@Injectable({ providedIn: 'root' })
export class EnvioRegistros {
  private readonly injector = inject(Injector);
  private readonly documento = inject(DOCUMENT);
  private readonly recientes = new Map<string, number>();
  private pausadoHasta = 0;

  enviar(nivel: NivelRegistro, mensaje: string, datos: DatosRegistro): void {
    try {
      const registro = this.crear(nivel, mensaje, datos);

      if (this.permitir(registro)) {
        new HttpClient(this.injector.get(HttpBackend))
          .post<void>(URL_REGISTROS, registro)
          .subscribe({ error: () => this.pausar() });
      }
    } catch {
      this.pausar();
    }
  }

  private permitir({ nivel, mensaje, ruta }: RegistroCliente): boolean {
    const ahora = Date.now();

    this.recientes.forEach((momento, clave) => {
      if (ahora - momento >= VENTANA_MS) {
        this.recientes.delete(clave);
      }
    });

    const clave = `${nivel}|${mensaje}|${ruta}`;

    if (
      ahora < this.pausadoHasta ||
      this.recientes.has(clave) ||
      this.recientes.size >= MAXIMO_POR_VENTANA
    ) {
      return false;
    }

    this.recientes.set(clave, ahora);
    return true;
  }

  private pausar(): void {
    this.pausadoHasta = Date.now() + VENTANA_MS;
  }

  private crear(nivel: NivelRegistro, mensaje: string, datos: DatosRegistro): RegistroCliente {
    const { error, correlacion } = datos;
    const tipo = error instanceof Error ? error.name : undefined;

    return {
      nivel,
      mensaje: ocultarDatosSensibles(this.describir(mensaje, datos)).slice(0, LONGITUD_MENSAJE),
      ruta: ocultarDatosSensibles(
        this.sinParametros(datos.ruta ?? this.documento.location.pathname),
      ).slice(0, LONGITUD_RUTA),
      tipo: tipo !== undefined && FORMATO_TIPO.test(tipo) ? tipo : undefined,
      correlacion:
        correlacion !== undefined && FORMATO_CORRELACION.test(correlacion)
          ? correlacion
          : undefined,
    };
  }

  private describir(mensaje: string, { metodo, url, estado, error }: DatosRegistro): string {
    if (url !== undefined) {
      return estado === 0
        ? `${metodo} ${this.sinParametros(url)} sin respuesta`
        : `${metodo} ${this.sinParametros(url)} respondió ${estado}`;
    }

    return error instanceof Error ? `${mensaje} ${error.message}` : mensaje;
  }

  private sinParametros(direccion: string): string {
    return new URL(direccion, this.documento.baseURI).pathname;
  }
}
