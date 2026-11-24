import { Rol } from '../nucleo/modelos/sesion';

export function crearToken(rol: Rol = 'Administrador', segundos = 3600, nombre = 'admin'): string {
  const exp = Math.floor(Date.now() / 1000) + segundos;
  const bytes = new TextEncoder().encode(JSON.stringify({ unique_name: nombre, role: rol, exp }));
  const contenido = btoa(String.fromCharCode(...bytes))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');

  return `cabecera.${contenido}.firma`;
}
