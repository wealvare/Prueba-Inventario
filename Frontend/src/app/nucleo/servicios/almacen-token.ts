import { Injectable } from '@angular/core';

const CLAVE = 'inventario.token';

@Injectable({ providedIn: 'root' })
export class AlmacenToken {
  leer(): string | null {
    try {
      return sessionStorage.getItem(CLAVE);
    } catch {
      return null;
    }
  }

  guardar(token: string): void {
    try {
      sessionStorage.setItem(CLAVE, token);
    } catch {
      return;
    }
  }

  borrar(): void {
    try {
      sessionStorage.removeItem(CLAVE);
    } catch {
      return;
    }
  }
}
