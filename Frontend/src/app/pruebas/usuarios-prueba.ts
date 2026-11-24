import { ResultadoPaginado } from '../compartido/modelos/resultado-paginado';
import { CuentaUsuario } from '../funcionalidades/usuarios/modelos/usuario';

export const USUARIO: CuentaUsuario = {
  id: 7,
  nombreUsuario: 'ana',
  correo: 'ana@ejemplo.com',
  rol: 'Consulta',
  activo: true,
};

export function crearPaginaUsuarios(
  cambios: Partial<ResultadoPaginado<CuentaUsuario>> = {},
): ResultadoPaginado<CuentaUsuario> {
  return {
    elementos: [
      {
        id: 1,
        nombreUsuario: 'admin',
        correo: 'a@ejemplo.com',
        rol: 'Administrador',
        activo: true,
      },
      USUARIO,
      { ...USUARIO, id: 8, nombreUsuario: 'eva', activo: false },
    ],
    pagina: 1,
    tamanoPagina: 10,
    totalElementos: 13,
    totalPaginas: 2,
    ...cambios,
  };
}
