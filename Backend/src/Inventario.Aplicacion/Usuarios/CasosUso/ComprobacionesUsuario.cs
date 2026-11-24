using Inventario.Aplicacion.Usuarios.Puertos.Salida;
using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Usuarios;

namespace Inventario.Aplicacion.Usuarios.CasosUso;

internal static class ComprobacionesUsuario
{
    public static async Task ComprobarUnicoAsync(
        this IRepositorioUsuarios repositorio,
        Usuario usuario,
        CancellationToken cancelacion)
    {
        int? idExcluido = usuario.Id == 0 ? null : usuario.Id;

        if (await repositorio.ExisteNombreUsuarioAsync(usuario.NombreUsuario, idExcluido, cancelacion))
            throw new ExcepcionNombreUsuarioDuplicado(usuario.NombreUsuario);

        if (await repositorio.ExisteCorreoAsync(usuario.Correo, idExcluido, cancelacion))
            throw new ExcepcionCorreoDuplicado(usuario.Correo);
    }

    public static async Task ComprobarAdministradorRestanteAsync(
        this IRepositorioUsuarios repositorio,
        Usuario usuario,
        bool eraAdministradorActivo,
        CancellationToken cancelacion)
    {
        if (eraAdministradorActivo
            && !usuario.EsAdministradorActivo
            && !await repositorio.ExisteOtroAdministradorActivoAsync(usuario.Id, cancelacion))
            throw new ExcepcionUltimoAdministrador();
    }
}
