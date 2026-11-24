using FluentValidation;
using Inventario.Aplicacion.Autenticacion.Puertos.Salida;
using Inventario.Aplicacion.Comun;
using Inventario.Aplicacion.Usuarios.CasosUso;
using Inventario.Aplicacion.Usuarios.Dtos;
using Inventario.Aplicacion.Usuarios.Puertos.Salida;
using Inventario.Aplicacion.Usuarios.Validadores;
using Inventario.Dominio.Excepciones;
using Inventario.Dominio.Usuarios;
using NSubstitute;

namespace Inventario.Pruebas.Aplicacion;

public sealed class UsuariosPruebas
{
    private const int IdActor = 1;
    private const int IdUsuario = 5;

    private static readonly SolicitudRegistro Registro = new("ana", "ana@inventario.local", "Clave123");
    private static readonly SolicitudCrearUsuario Creacion =
        new("ana", "ana@inventario.local", "Clave123", RolUsuario.Administrador);
    private static readonly SolicitudActualizarUsuario Actualizacion =
        new("eva", "eva@inventario.local", RolUsuario.Consulta, true, null);

    private readonly IRepositorioUsuarios _repositorio = Substitute.For<IRepositorioUsuarios>();
    private readonly IHasheadorContrasena _hasheador = Substitute.For<IHasheadorContrasena>();

    public UsuariosPruebas()
    {
        _hasheador.Hashear(Arg.Any<string>()).Returns(llamada => $"hash:{llamada.Arg<string>()}");
        _repositorio.AgregarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>())
            .Returns(llamada => llamada.Arg<Usuario>());
        _repositorio.ActualizarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>())
            .Returns(llamada => llamada.Arg<Usuario>());
        Existe(RolUsuario.Administrador);
    }

    private RegistrarUsuario Registrar => new(_repositorio, _hasheador, new ValidadorRegistro());
    private CrearUsuario Crear => new(_repositorio, _hasheador, new ValidadorCrearUsuario());
    private ActualizarUsuario Actualizar => new(_repositorio, _hasheador, new ValidadorActualizarUsuario());
    private DesactivarUsuario Desactivar => new(_repositorio);
    private ListarUsuarios Listar => new(_repositorio, new ValidadorFiltroUsuarios());

    private void Existe(RolUsuario rol, bool activo = true, bool otroAdministrador = true)
    {
        _repositorio.ObtenerPorIdAsync(IdUsuario, Arg.Any<CancellationToken>()).Returns(
            Usuario.Reconstruir(IdUsuario, "ana", "ana@inventario.local", "hash", rol, activo));
        _repositorio.ExisteOtroAdministradorActivoAsync(IdUsuario, Arg.Any<CancellationToken>())
            .Returns(otroAdministrador);
    }

    private Task GuardadoAsync(Func<Usuario, bool> condicion) => _repositorio.Received(1).ActualizarAsync(
        Arg.Is<Usuario>(usuario => condicion(usuario)), Arg.Any<CancellationToken>());

    private Task NadaGuardadoAsync() => _repositorio.DidNotReceiveWithAnyArgs().ActualizarAsync(default!, default);

    [Fact]
    public async Task Registrar_SiempreCreaUnUsuarioDeConsultaActivo()
    {
        var creado = await Registrar.EjecutarAsync(Registro, default);

        Assert.Equal(("ana", "Consulta", true), (creado.NombreUsuario, creado.Rol, creado.Activo));
        await _repositorio.Received(1).AgregarAsync(
            Arg.Is<Usuario>(usuario => usuario.HashContrasena == "hash:Clave123"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Crear_UsaElRolIndicado() =>
        Assert.Equal("Administrador", (await Crear.EjecutarAsync(Creacion, default)).Rol);

    [Fact]
    public async Task NombreDuplicado_FallaSinGuardar()
    {
        _repositorio.ExisteNombreUsuarioAsync("ana", null, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ExcepcionNombreUsuarioDuplicado>(() => Registrar.EjecutarAsync(Registro, default));
        await Assert.ThrowsAsync<ExcepcionNombreUsuarioDuplicado>(() => Crear.EjecutarAsync(Creacion, default));
        await _repositorio.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
    }

    [Fact]
    public async Task CorreoDuplicado_FallaSinGuardar()
    {
        _repositorio.ExisteCorreoAsync("ana@inventario.local", null, Arg.Any<CancellationToken>()).Returns(true);
        _repositorio.ExisteCorreoAsync("eva@inventario.local", IdUsuario, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ExcepcionCorreoDuplicado>(() => Registrar.EjecutarAsync(Registro, default));
        await Assert.ThrowsAsync<ExcepcionCorreoDuplicado>(() =>
            Actualizar.EjecutarAsync(IdUsuario, Actualizacion, IdActor, default));
        await _repositorio.DidNotReceiveWithAnyArgs().AgregarAsync(default!, default);
        await NadaGuardadoAsync();
    }

    [Theory]
    [InlineData("", "ana@inventario.local", "Clave123")]
    [InlineData("a b", "ana@inventario.local", "Clave123")]
    [InlineData("ana", "correo", "Clave123")]
    [InlineData("ana", "ana@inventario.local", "clave123")]
    [InlineData(null, "ana@inventario.local", "Clave123")]
    [InlineData("ana", null, "Clave123")]
    [InlineData("ana", "ana@inventario.local", null)]
    public async Task DatosInvalidos_FallanLaValidacion(string? nombre, string? correo, string? contrasena)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Registrar.EjecutarAsync(new(nombre!, correo!, contrasena!), default));
        await Assert.ThrowsAsync<ValidationException>(() =>
            Crear.EjecutarAsync(new(nombre!, correo!, contrasena!, RolUsuario.Consulta), default));
    }

    [Theory]
    [InlineData(null)]
    [InlineData((RolUsuario)99)]
    public async Task RolInvalido_FallaLaValidacion(RolUsuario? rol)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Crear.EjecutarAsync(Creacion with { Rol = rol }, default));
        await Assert.ThrowsAsync<ValidationException>(() =>
            Actualizar.EjecutarAsync(IdUsuario, Actualizacion with { Rol = rol }, IdActor, default));
    }

    [Fact]
    public async Task Actualizar_SinActivo_FallaLaValidacion() =>
        await Assert.ThrowsAsync<ValidationException>(() =>
            Actualizar.EjecutarAsync(IdUsuario, Actualizacion with { Activo = null }, IdActor, default));

    [Fact]
    public async Task Actualizar_SinContrasena_ConservaElHash()
    {
        var resultado = await Actualizar.EjecutarAsync(IdUsuario, Actualizacion, IdActor, default);

        Assert.Equal(new UsuarioDto(IdUsuario, "eva", "eva@inventario.local", "Consulta", true), resultado);
        await GuardadoAsync(usuario => usuario.HashContrasena == "hash");
    }

    [Fact]
    public async Task Actualizar_ConContrasena_CambiaElHash()
    {
        await Actualizar.EjecutarAsync(IdUsuario, Actualizacion with { Contrasena = "Nueva456" }, IdActor, default);

        await GuardadoAsync(usuario => usuario.HashContrasena == "hash:Nueva456");
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    public async Task Actualizar_ConContrasenaInvalida_FallaLaValidacion(string contrasena) =>
        await Assert.ThrowsAsync<ValidationException>(() =>
            Actualizar.EjecutarAsync(IdUsuario, Actualizacion with { Contrasena = contrasena }, IdActor, default));

    [Theory]
    [InlineData(RolUsuario.Consulta, true)]
    [InlineData(RolUsuario.Administrador, false)]
    public async Task Actualizar_LaCuentaPropia_NoPermitePerderElAcceso(RolUsuario rol, bool activo)
    {
        await Assert.ThrowsAsync<ExcepcionCuentaPropia>(() => Actualizar.EjecutarAsync(
            IdUsuario, Actualizacion with { Rol = rol, Activo = activo }, IdUsuario, default));
        await NadaGuardadoAsync();
    }

    [Theory]
    [InlineData(RolUsuario.Consulta, true)]
    [InlineData(RolUsuario.Administrador, false)]
    public async Task Actualizar_AlUltimoAdministrador_Falla(RolUsuario rol, bool activo)
    {
        Existe(RolUsuario.Administrador, otroAdministrador: false);

        await Assert.ThrowsAsync<ExcepcionUltimoAdministrador>(() => Actualizar.EjecutarAsync(
            IdUsuario, Actualizacion with { Rol = rol, Activo = activo }, IdActor, default));
        await NadaGuardadoAsync();
    }

    [Fact]
    public async Task Desactivar_GuardaElUsuarioInactivo()
    {
        await Desactivar.EjecutarAsync(IdUsuario, IdActor, default);

        await GuardadoAsync(usuario => !usuario.Activo);
    }

    [Fact]
    public async Task Desactivar_LaCuentaPropiaOElUltimoAdministrador_Falla()
    {
        await Assert.ThrowsAsync<ExcepcionCuentaPropia>(() => Desactivar.EjecutarAsync(IdUsuario, IdUsuario, default));

        Existe(RolUsuario.Administrador, otroAdministrador: false);
        await Assert.ThrowsAsync<ExcepcionUltimoAdministrador>(() =>
            Desactivar.EjecutarAsync(IdUsuario, IdActor, default));
        await NadaGuardadoAsync();
    }

    [Fact]
    public async Task UsuarioInexistente_Falla()
    {
        await Assert.ThrowsAsync<ExcepcionUsuarioNoEncontrado>(() =>
            new ObtenerUsuario(_repositorio).EjecutarAsync(9, default));
        await Assert.ThrowsAsync<ExcepcionUsuarioNoEncontrado>(() => Desactivar.EjecutarAsync(9, IdActor, default));
        await Assert.ThrowsAsync<ExcepcionUsuarioNoEncontrado>(() =>
            Actualizar.EjecutarAsync(9, Actualizacion, IdActor, default));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Listar_ConPaginacionInvalida_FallaLaValidacion(int pagina, int tamano) =>
        await Assert.ThrowsAsync<ValidationException>(() => Listar.EjecutarAsync(new(pagina, tamano), default));

    [Fact]
    public async Task Listar_DevuelveLaPagina()
    {
        var esperado = new ResultadoPaginado<UsuarioDto>([], 1, 10, 0);
        _repositorio.ListarAsync(new FiltroUsuarios(), Arg.Any<CancellationToken>()).Returns(esperado);

        Assert.Same(esperado, await Listar.EjecutarAsync(new FiltroUsuarios(), default));
    }

    [Theory]
    [InlineData(RolUsuario.Administrador, true, true)]
    [InlineData(RolUsuario.Administrador, false, false)]
    [InlineData(RolUsuario.Consulta, true, false)]
    public async Task VerificarAdministrador_ExigeRolYEstadoActuales(RolUsuario rol, bool activo, bool esperado)
    {
        Existe(rol, activo);

        Assert.Equal(esperado, await new VerificarAdministrador(_repositorio).EjecutarAsync(IdUsuario, default));
    }
}
