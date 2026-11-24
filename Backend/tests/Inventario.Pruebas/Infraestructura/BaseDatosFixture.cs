using Inventario.Infraestructura.Configuracion;
using Inventario.Infraestructura.Persistencia;
using Inventario.Infraestructura.Persistencia.Conexion;
using Inventario.Infraestructura.Persistencia.Productos;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Inventario.Pruebas.Infraestructura;

public sealed class BaseDatosFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _contenedor =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public OpcionesBaseDatos Opciones { get; private set; } = new(string.Empty);
    public IFabricaConexion Fabrica => new FabricaConexion(Opciones);

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();
        Opciones = new OpcionesBaseDatos(CadenaPara("InventarioPruebas"));
        new MigradorEsquema(Opciones).Ejecutar();
    }

    public Task DisposeAsync() => _contenedor.DisposeAsync().AsTask();

    public RepositorioProductos CrearRepositorio() => new(Fabrica, new EjecutorTransaccion(Fabrica));

    public string CadenaPara(string baseDatos) =>
        new SqlConnectionStringBuilder(_contenedor.GetConnectionString()) { InitialCatalog = baseDatos }.ConnectionString;
}

[CollectionDefinition("BaseDatos")]
public sealed class ColeccionBaseDatos : ICollectionFixture<BaseDatosFixture>;
