export type Rol = 'Administrador' | 'Consulta';

export interface SolicitudInicioSesion {
  nombreUsuario: string;
  contrasena: string;
}

export interface SolicitudRegistro {
  nombreUsuario: string;
  correo: string;
  contrasena: string;
}

export interface RespuestaInicioSesion {
  token: string;
  expiracion: string;
  nombreUsuario: string;
  rol: Rol;
}

export interface Usuario {
  nombreUsuario: string;
  rol: Rol;
  expiracion: number;
}
