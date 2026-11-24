using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Usuarios;

namespace Inventario.Pruebas.Dominio;

public sealed class UsuarioPruebas
{
    [Fact]
    public void Crear_ConDatosValidos_AsignaLosDatos()
    {
        var usuario = new Usuario(" admin ", "admin@inventario.local", "hash", RolUsuario.Administrador);

        Assert.Equal("admin", usuario.NombreUsuario);
        Assert.Equal(RolUsuario.Administrador, usuario.Rol);
    }

    [Theory]
    [InlineData("", "admin@inventario.local", "hash")]
    [InlineData("admin", "correo-invalido", "hash")]
    [InlineData("admin", "admin@inventario.local", "")]
    public void Crear_ConDatosInvalidos_Falla(string nombre, string correo, string hash) =>
        Assert.Throws<ExcepcionReglaNegocio>(() => new Usuario(nombre, correo, hash, RolUsuario.Consulta));

    [Fact]
    public void Crear_ConRolInexistente_Falla() =>
        Assert.Throws<ExcepcionReglaNegocio>(() =>
            new Usuario("admin", "admin@inventario.local", "hash", (RolUsuario)99));
}
