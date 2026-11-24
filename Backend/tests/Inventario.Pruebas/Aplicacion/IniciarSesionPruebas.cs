using FluentValidation;
using Inventario.Aplicacion.Autenticacion.CasosUso;
using Inventario.Aplicacion.Autenticacion.Dtos;
using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using Inventario.Aplicacion.Autenticacion.Validadores;
using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Usuarios;
using NSubstitute;

namespace Inventario.Pruebas.Aplicacion;

public sealed class IniciarSesionPruebas
{
    private readonly IRepositorioUsuarios _repositorio = Substitute.For<IRepositorioUsuarios>();
    private readonly IHasheadorContrasena _hasheador = Substitute.For<IHasheadorContrasena>();
    private readonly IServicioToken _servicioToken = Substitute.For<IServicioToken>();
    private readonly Usuario _usuario = new("admin", "admin@inventario.local", "hash", RolUsuario.Administrador);
    private readonly IniciarSesion _casoUso;

    public IniciarSesionPruebas()
    {
        _casoUso = new IniciarSesion(_repositorio, _hasheador, _servicioToken, new ValidadorInicioSesion());
        _repositorio.ObtenerPorNombreUsuarioAsync("admin", Arg.Any<CancellationToken>()).Returns(_usuario);
        _hasheador.Verificar("clave", "hash").Returns(true);
        _hasheador.HashFicticio.Returns("ficticio");
    }

    [Fact]
    public async Task ConCredencialesValidas_DevuelveToken()
    {
        var expiracion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _servicioToken.Generar(_usuario).Returns(new TokenAcceso("token", expiracion));

        var respuesta = await _casoUso.EjecutarAsync(new("admin", "clave"), CancellationToken.None);

        Assert.Equal(new RespuestaInicioSesion("token", expiracion, "admin", "Administrador"), respuesta);
    }

    [Theory]
    [InlineData("admin", "incorrecta")]
    [InlineData("desconocido", "clave")]
    public async Task ConCredencialesInvalidas_FallaSinGenerarToken(string nombreUsuario, string contrasena)
    {
        await Assert.ThrowsAsync<ExcepcionCredencialesInvalidas>(() =>
            _casoUso.EjecutarAsync(new(nombreUsuario, contrasena), CancellationToken.None));
        _servicioToken.DidNotReceiveWithAnyArgs().Generar(default!);
    }

    [Fact]
    public async Task ConUsuarioInexistente_VerificaElHashFicticioYFalla()
    {
        _hasheador.Verificar("clave", "ficticio").Returns(true);

        await Assert.ThrowsAsync<ExcepcionCredencialesInvalidas>(() =>
            _casoUso.EjecutarAsync(new("desconocido", "clave"), CancellationToken.None));

        _hasheador.Received(1).Verificar("clave", "ficticio");
    }

    [Theory]
    [InlineData("", "clave")]
    [InlineData("admin", null)]
    public async Task SinDatosObligatorios_FallaLaValidacion(string nombreUsuario, string? contrasena) =>
        await Assert.ThrowsAsync<ValidationException>(() =>
            _casoUso.EjecutarAsync(new(nombreUsuario, contrasena!), CancellationToken.None));
}
