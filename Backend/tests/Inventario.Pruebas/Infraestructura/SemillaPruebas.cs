using Dapper;
using Inventario.Dominio.Usuarios;
using Inventario.Infraestructura.Configuracion;
using Inventario.Infraestructura.Persistencia;
using Inventario.Infraestructura.Persistencia.Conexion;
using Inventario.Infraestructura.Persistencia.Productos;
using Inventario.Infraestructura.Persistencia.Semilla;
using Inventario.Infraestructura.Persistencia.Usuarios;
using Inventario.Infraestructura.Seguridad;

namespace Inventario.Pruebas.Infraestructura;

[Collection("BaseDatos")]
[Trait("Categoria", "Integracion")]
public sealed class SemillaPruebas(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Inicializador_CreaUsuariosYProductosUnaSolaVez()
    {
        var opciones = new OpcionesBaseDatos(baseDatos.CadenaPara("InventarioSemilla"));
        var fabrica = new FabricaConexion(opciones);
        var usuarios = new RepositorioUsuarios(fabrica);
        var productos = new RepositorioProductos(fabrica, new EjecutorTransaccion(fabrica));
        var hasheador = new HasheadorContrasena();
        var sembrador = new SembradorDatos(fabrica, usuarios, productos, hasheador, new OpcionesSemilla(
        [
            new("admin", "admin@inventario.local", "contrasena-a", RolUsuario.Administrador),
            new("consulta", "consulta@inventario.local", "contrasena-c", RolUsuario.Consulta)
        ]));
        var inicializador = new InicializadorBaseDatos(new MigradorEsquema(opciones), sembrador);

        await inicializador.EjecutarAsync(default);
        await inicializador.EjecutarAsync(default);

        var administrador = await usuarios.ObtenerPorNombreUsuarioAsync("admin", default);
        var consulta = await usuarios.ObtenerPorNombreUsuarioAsync("consulta", default);
        var pagina = await productos.ListarAsync(new(TamanoPagina: 100), default);
        await using var conexion = await fabrica.AbrirAsync(default);
        Assert.Equal(2, await conexion.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Usuarios"));
        Assert.Equal((RolUsuario.Administrador, RolUsuario.Consulta), (administrador!.Rol, consulta!.Rol));
        Assert.True(administrador.Id > 0);
        Assert.True(hasheador.Verificar("contrasena-a", administrador.HashContrasena));
        Assert.Null(await usuarios.ObtenerPorNombreUsuarioAsync("desconocido", default));
        Assert.Equal(15, pagina.TotalElementos);
        Assert.All(pagina.Elementos, producto =>
        {
            Assert.True(producto.CantidadLotes >= 2);
            Assert.True(producto.PrecioMinimo < producto.PrecioMaximo);
        });
    }
}
