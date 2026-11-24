using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Seguridad;

namespace Inventario.Pruebas.Infraestructura;

public sealed class HasheadorContrasenaPruebas
{
    private readonly HasheadorContrasena _hasheador = new();

    [Fact]
    public void Verificar_SoloAceptaLaContrasenaOriginal()
    {
        var hash = _hasheador.Hashear("una-clave");

        Assert.True(_hasheador.Verificar("una-clave", hash));
        Assert.False(_hasheador.Verificar("otra-clave", hash));
        Assert.True(hash.Length <= Usuario.LongitudHashContrasena);
    }

    [Fact]
    public void Verificar_DistingueContrasenasDeMasDe72Bytes()
    {
        var comun = new string('a', 80);

        Assert.False(_hasheador.Verificar(comun + "2", _hasheador.Hashear(comun + "1")));
    }

    [Fact]
    public void HashFicticio_TieneElMismoCosteQueUnoReal() =>
        Assert.Equal(_hasheador.Hashear("una-clave")[..7], _hasheador.HashFicticio[..7]);

    [Fact]
    public void Hashear_UsaUnaSalDistintaCadaVez() =>
        Assert.NotEqual(_hasheador.Hashear("una-clave"), _hasheador.Hashear("una-clave"));

    [Theory]
    [InlineData("")]
    [InlineData("texto")]
    [InlineData("$2a$12$corto")]
    public void Verificar_ConHashMalFormado_DevuelveFalso(string hash) =>
        Assert.False(_hasheador.Verificar("una-clave", hash));
}
