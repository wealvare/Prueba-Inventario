import { inject, Injectable } from '@angular/core';
import { DatosRegistro } from '../modelos/registro-cliente';
import { EnvioRegistros } from './envio-registros';

@Injectable({ providedIn: 'root' })
export class ServicioRegistro {
  private readonly envio = inject(EnvioRegistros);

  advertencia(mensaje: string, datos: DatosRegistro = {}): void {
    console.warn(this.prefijo(), mensaje, datos);
  }

  advertenciaImportante(mensaje: string, datos: DatosRegistro = {}): void {
    this.advertencia(mensaje, datos);
    this.envio.enviar('Advertencia', mensaje, datos);
  }

  error(mensaje: string, datos: DatosRegistro = {}): void {
    console.error(this.prefijo(), mensaje, datos);
    this.envio.enviar('Error', mensaje, datos);
  }

  private prefijo(): string {
    return `[Inventario ${new Date().toISOString()}]`;
  }
}
