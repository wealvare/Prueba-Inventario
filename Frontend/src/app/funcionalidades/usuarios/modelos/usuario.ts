import { Rol } from '../../../nucleo/modelos/sesion';

export interface CuentaUsuario {
  id: number;
  nombreUsuario: string;
  correo: string;
  rol: Rol;
  activo: boolean;
}

export interface SolicitudCrearUsuario {
  nombreUsuario: string;
  correo: string;
  contrasena: string;
  rol: Rol;
}

export interface SolicitudActualizarUsuario {
  nombreUsuario: string;
  correo: string;
  rol: Rol;
  activo: boolean;
  contrasena: string | null;
}
