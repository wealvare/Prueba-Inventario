using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Usuarios;

namespace Inventario.Pruebas.Dominio;

public sealed class UsuarioPruebas
{
    private static Usuario Existente() =>
        Usuario.Reconstruir(5, "ana", "ana@inventario.local", "hash", RolUsuario.Administrador, true);

    [Fact]
    public void Crear_ConDatosValidos_AsignaLosDatosYQuedaActivo()
    {
        var usuario = new Usuario(" admin ", " admin@inventario.local ", "hash", RolUsuario.Administrador);

        Assert.Equal(("admin", "admin@inventario.local"), (usuario.NombreUsuario, usuario.Correo));
        Assert.Equal(RolUsuario.Administrador, usuario.Rol);
        Assert.True(usuario.EsAdministradorActivo);
    }

    [Theory]
    [InlineData("", "admin@inventario.local", "hash")]
    [InlineData("ab", "admin@inventario.local", "hash")]
    [InlineData("con espacio", "admin@inventario.local", "hash")]
    [InlineData("<script>", "admin@inventario.local", "hash")]
    [InlineData("admin", "correo-invalido", "hash")]
    [InlineData("admin", "admin@sindominio", "hash")]
    [InlineData("admin", "Ana <ana@inventario.local>", "hash")]
    [InlineData("admin", "admin@inventario.local", "")]
    public void Crear_ConDatosInvalidos_Falla(string nombre, string correo, string hash) =>
        Assert.Throws<ExcepcionReglaNegocio>(() => new Usuario(nombre, correo, hash, RolUsuario.Consulta));

    [Fact]
    public void Crear_ConTextosDemasiadoLargos_Falla()
    {
        Assert.Throws<ExcepcionReglaNegocio>(() =>
            new Usuario(new string('a', 51), "a@inventario.local", "hash", RolUsuario.Consulta));
        Assert.Throws<ExcepcionReglaNegocio>(() =>
            new Usuario("admin", new string('a', 150) + "@inventario.local", "hash", RolUsuario.Consulta));
    }

    [Fact]
    public void Crear_ConRolInexistente_Falla() =>
        Assert.Throws<ExcepcionReglaNegocio>(() =>
            new Usuario("admin", "admin@inventario.local", "hash", (RolUsuario)99));

    [Fact]
    public void Actualizar_CambiaLosDatosDeOtroUsuario()
    {
        var usuario = Existente();

        usuario.Actualizar("eva", "eva@inventario.local", RolUsuario.Consulta, false, idActor: 1);

        Assert.Equal(("eva", "eva@inventario.local", RolUsuario.Consulta, false),
            (usuario.NombreUsuario, usuario.Correo, usuario.Rol, usuario.Activo));
    }

    [Theory]
    [InlineData(RolUsuario.Consulta, true)]
    [InlineData(RolUsuario.Administrador, false)]
    public void Actualizar_LaCuentaPropia_NoPermitePerderElAcceso(RolUsuario rol, bool activo)
    {
        var usuario = Existente();

        Assert.Throws<ExcepcionCuentaPropia>(() => usuario.Actualizar("ana", "ana@inventario.local", rol, activo, 5));
        Assert.True(usuario.EsAdministradorActivo);
    }

    [Fact]
    public void Actualizar_LaCuentaPropia_PermiteCambiarNombreYCorreo()
    {
        var usuario = Existente();

        usuario.Actualizar("eva", "eva@inventario.local", RolUsuario.Administrador, true, 5);

        Assert.Equal("eva", usuario.NombreUsuario);
    }

    [Fact]
    public void Desactivar_SoloPermiteOtrasCuentas()
    {
        var usuario = Existente();

        Assert.Throws<ExcepcionCuentaPropia>(() => usuario.Desactivar(5));
        usuario.Desactivar(1);

        Assert.False(usuario.Activo);
    }

    [Theory]
    [InlineData("Clave123", true)]
    [InlineData("Ñandú123", true)]
    [InlineData("Clave12", false)]
    [InlineData("clave123", false)]
    [InlineData("CLAVE123", false)]
    [InlineData("ClaveClave", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void PoliticaContrasena_ExigeLongitudMayusculaMinusculaYNumero(string? contrasena, bool cumple)
    {
        Assert.Equal(cumple, PoliticaContrasena.Cumple(contrasena));

        if (!cumple)
            Assert.Throws<ExcepcionReglaNegocio>(() => PoliticaContrasena.Validar(contrasena));
    }

    [Fact]
    public void PoliticaContrasena_LimitaLaLongitudMaxima()
    {
        Assert.True(PoliticaContrasena.Cumple("Aa1" + new string('x', 97)));
        Assert.False(PoliticaContrasena.Cumple("Aa1" + new string('x', 98)));
    }
}
