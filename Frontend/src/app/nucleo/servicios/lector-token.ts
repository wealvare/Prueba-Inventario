import { Rol, Usuario } from '../modelos/sesion';

const ROLES: readonly Rol[] = ['Administrador', 'Consulta'];

export function leerToken(token: string): Usuario | null {
  try {
    const contenido = JSON.parse(decodificarBase64Url(token.split('.')[1]));
    const { unique_name: nombreUsuario, role: rol, exp: expiracion } = contenido;

    if (
      typeof nombreUsuario !== 'string' ||
      typeof expiracion !== 'number' ||
      !ROLES.includes(rol)
    ) {
      return null;
    }

    return { nombreUsuario, rol, expiracion: expiracion * 1000 };
  } catch {
    return null;
  }
}

function decodificarBase64Url(texto: string): string {
  const base64 = texto.replace(/-/g, '+').replace(/_/g, '/');
  const bytes = Uint8Array.from(atob(base64), (caracter) => caracter.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}
