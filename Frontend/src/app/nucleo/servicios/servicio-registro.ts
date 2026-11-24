import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ServicioRegistro {
  advertencia(mensaje: string, datos: Record<string, unknown> = {}): void {
    console.warn(this.prefijo(), mensaje, datos);
  }

  error(mensaje: string, datos: Record<string, unknown> = {}): void {
    console.error(this.prefijo(), mensaje, datos);
  }

  private prefijo(): string {
    return `[Inventario ${new Date().toISOString()}]`;
  }
}
