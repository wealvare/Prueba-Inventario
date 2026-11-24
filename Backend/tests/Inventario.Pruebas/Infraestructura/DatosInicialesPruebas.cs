using Dapper;
using Inventario.Dominio.Productos;
using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Configuracion;
using Inventario.Infraestructura.Persistencia;
using Inventario.Infraestructura.Persistencia.Conexion;
using Inventario.Infraestructura.Persistencia.DatosIniciales;
using Inventario.Infraestructura.Persistencia.Productos;
using Inventario.Infraestructura.Persistencia.Usuarios;
using Inventario.Infraestructura.Seguridad;
using Microsoft.Data.SqlClient;

namespace Inventario.Pruebas.Infraestructura;

[Collection("BaseDatos")]
[Trait("Categoria", "Integracion")]
public sealed class DatosInicialesPruebas(BaseDatosFixture baseDatos)
{
    private readonly HasheadorContrasena _hasheador = new();

    private (FabricaConexion Fabrica, MigradorEsquema Migrador, CargadorDatosIniciales Cargador) Preparar(
        string nombre,
        string correoConsulta = "consulta@inventario.local")
    {
        var opciones = new OpcionesBaseDatos(baseDatos.CadenaPara(nombre));
        var fabrica = new FabricaConexion(opciones);
        var cargador = new CargadorDatosIniciales(new EjecutorTransaccion(fabrica), _hasheador, new OpcionesUsuariosIniciales(
        [
            new("admin", "admin@inventario.local", "contrasena-a", RolUsuario.Administrador),
            new("consulta", correoConsulta, "contrasena-c", RolUsuario.Consulta)
        ]));

        return (fabrica, new MigradorEsquema(opciones), cargador);
    }

    private static async Task<(int Usuarios, int Productos, int Lotes)> ContarAsync(IFabricaConexion fabrica)
    {
        await using var conexion = await fabrica.AbrirAsync(default);
        return await conexion.QuerySingleAsync<(int, int, int)>("""
            SELECT (SELECT COUNT(*) FROM dbo.Usuarios),
                (SELECT COUNT(*) FROM dbo.Productos),
                (SELECT COUNT(*) FROM dbo.LotesProducto)
            """);
    }

    [Fact]
    public async Task Inicializador_CreaUsuariosYProductosUnaSolaVez()
    {
        var (fabrica, migrador, cargador) = Preparar("DatosInicialesRepetida");
        var inicializador = new InicializadorBaseDatos(migrador, cargador);

        await inicializador.EjecutarAsync(default);
        await inicializador.EjecutarAsync(default);

        var usuarios = new RepositorioUsuarios(fabrica, new EjecutorTransaccion(fabrica));
        var administrador = await usuarios.ObtenerPorNombreUsuarioAsync("admin", default);
        var consulta = await usuarios.ObtenerPorNombreUsuarioAsync("consulta", default);
        Assert.Equal((2, 15, 35), await ContarAsync(fabrica));
        Assert.Equal((RolUsuario.Administrador, RolUsuario.Consulta), (administrador!.Rol, consulta!.Rol));
        Assert.True(_hasheador.Verificar("contrasena-a", administrador.HashContrasena));
        Assert.Null(await usuarios.ObtenerPorNombreUsuarioAsync("desconocido", default));
    }

    [Fact]
    public async Task Cargador_EnParalelo_InsertaUnaSolaVez()
    {
        var (fabrica, migrador, cargador) = Preparar("DatosInicialesParalela");
        migrador.Ejecutar();

        await Task.WhenAll(cargador.EjecutarAsync(default), cargador.EjecutarAsync(default));

        Assert.Equal((2, 15, 35), await ContarAsync(fabrica));
    }

    [Fact]
    public async Task Cargador_ConProductosExistentes_NoInsertaProductos()
    {
        var (fabrica, migrador, cargador) = Preparar("DatosInicialesConProductos");
        migrador.Ejecutar();
        await new RepositorioProductos(fabrica, new EjecutorTransaccion(fabrica))
            .AgregarAsync(new Producto("PROPIO-1", "Propio", null, "Otra"), default);

        await cargador.EjecutarAsync(default);

        Assert.Equal((2, 1, 0), await ContarAsync(fabrica));
    }

    [Fact]
    public async Task Cargador_SiFalla_NoInsertaNada()
    {
        var (fabrica, migrador, cargador) = Preparar("DatosInicialesFallida", "admin@inventario.local");
        migrador.Ejecutar();

        await Assert.ThrowsAsync<SqlException>(() => cargador.EjecutarAsync(default));

        Assert.Equal((0, 0, 0), await ContarAsync(fabrica));
    }
}
