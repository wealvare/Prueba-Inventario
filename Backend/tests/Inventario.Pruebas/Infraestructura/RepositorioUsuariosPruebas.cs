using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Configuracion;
using Inventario.Infraestructura.Persistencia;
using Inventario.Infraestructura.Persistencia.Conexion;
using Inventario.Infraestructura.Persistencia.Usuarios;

namespace Inventario.Pruebas.Infraestructura;

[Collection("BaseDatos")]
[Trait("Categoria", "Integracion")]
public sealed class RepositorioUsuariosPruebas(BaseDatosFixture baseDatos)
{
    private RepositorioUsuarios Preparar()
    {
        var opciones = new OpcionesBaseDatos(baseDatos.CadenaPara($"Usuarios{Guid.NewGuid():N}"));
        new MigradorEsquema(opciones).Ejecutar();
        var fabrica = new FabricaConexion(opciones);
        return new RepositorioUsuarios(fabrica, new EjecutorTransaccion(fabrica));
    }

    private static Task<Usuario> AgregarAsync(
        RepositorioUsuarios repositorio,
        string nombre,
        RolUsuario rol = RolUsuario.Administrador) =>
        repositorio.AgregarAsync(new Usuario(nombre, $"{nombre}@inventario.local", "hash", rol), default);

    [Fact]
    public async Task Guardar_ConservaLosDatos()
    {
        var repositorio = Preparar();
        await AgregarAsync(repositorio, "admin");
        var usuario = await AgregarAsync(repositorio, "ana", RolUsuario.Consulta);

        usuario.Actualizar("eva", "eva@inventario.local", RolUsuario.Consulta, false, 0);
        usuario.CambiarContrasena("otro");
        await repositorio.ActualizarAsync(usuario, default);

        var leido = (await repositorio.ObtenerPorIdAsync(usuario.Id, default))!;
        Assert.Equal(("eva", "eva@inventario.local", "otro", RolUsuario.Consulta, false),
            (leido.NombreUsuario, leido.Correo, leido.HashContrasena, leido.Rol, leido.Activo));
        Assert.Null(await repositorio.ObtenerPorIdAsync(int.MaxValue, default));
    }

    [Fact]
    public async Task Duplicados_SeDetectanSinDistinguirMayusculas()
    {
        var repositorio = Preparar();
        var ana = await AgregarAsync(repositorio, "ana");

        Assert.True(await repositorio.ExisteNombreUsuarioAsync("ANA", null, default));
        Assert.True(await repositorio.ExisteCorreoAsync("ANA@inventario.local", null, default));
        Assert.False(await repositorio.ExisteNombreUsuarioAsync("ana", ana.Id, default));
        Assert.False(await repositorio.ExisteCorreoAsync("ana@inventario.local", ana.Id, default));
        await Assert.ThrowsAsync<ExcepcionNombreUsuarioDuplicado>(() =>
            repositorio.AgregarAsync(new("ANA", "otra@inventario.local", "hash", RolUsuario.Consulta), default));
        await Assert.ThrowsAsync<ExcepcionCorreoDuplicado>(() =>
            repositorio.AgregarAsync(new("otra", "ANA@inventario.local", "hash", RolUsuario.Consulta), default));
    }

    [Fact]
    public async Task Listar_PaginaPorNombre()
    {
        var repositorio = Preparar();
        foreach (var nombre in new[] { "carla", "ana", "beto" })
            await AgregarAsync(repositorio, nombre);

        var primera = await repositorio.ListarAsync(new(1, 2), default);
        var segunda = await repositorio.ListarAsync(new(2, 2), default);

        Assert.Equal(["ana", "beto"], primera.Elementos.Select(usuario => usuario.NombreUsuario));
        Assert.Equal("carla", Assert.Single(segunda.Elementos).NombreUsuario);
        Assert.Equal((3, 2), (primera.TotalElementos, primera.TotalPaginas));
    }

    [Fact]
    public async Task Actualizar_AlUltimoAdministrador_FallaSinCambios()
    {
        var repositorio = Preparar();
        var administrador = await AgregarAsync(repositorio, "admin");
        await AgregarAsync(repositorio, "consulta", RolUsuario.Consulta);

        administrador.Desactivar(0);

        await Assert.ThrowsAsync<ExcepcionUltimoAdministrador>(() =>
            repositorio.ActualizarAsync(administrador, default));
        Assert.True((await repositorio.ObtenerPorIdAsync(administrador.Id, default))!.Activo);
        Assert.False(await repositorio.ExisteOtroAdministradorActivoAsync(administrador.Id, default));
    }

    [Fact]
    public async Task Desactivar_ATodosLosAdministradoresEnParalelo_ConservaUno()
    {
        var repositorio = Preparar();
        var administradores = new List<Usuario>();
        for (var indice = 0; indice < 5; indice++)
            administradores.Add(await AgregarAsync(repositorio, $"admin{indice}"));
        administradores.ForEach(usuario => usuario.Desactivar(0));

        var errores = await Task.WhenAll(administradores.Select(usuario =>
            Record.ExceptionAsync(() => repositorio.ActualizarAsync(usuario, default))));

        Assert.IsType<ExcepcionUltimoAdministrador>(Assert.Single(errores, error => error is not null));
        var pagina = await repositorio.ListarAsync(new(), default);
        Assert.Single(pagina.Elementos, usuario => usuario.Activo);
    }
}
