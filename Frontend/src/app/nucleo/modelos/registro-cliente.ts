export type NivelRegistro = 'Advertencia' | 'Error';

export interface DatosRegistro {
  metodo?: string;
  url?: string;
  estado?: number;
  correlacion?: string;
  ruta?: string;
  error?: unknown;
}

export interface RegistroCliente {
  nivel: NivelRegistro;
  mensaje: string;
  ruta: string;
  tipo?: string;
  correlacion?: string;
}
