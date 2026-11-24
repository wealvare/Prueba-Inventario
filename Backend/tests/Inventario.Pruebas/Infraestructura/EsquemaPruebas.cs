using Dapper;
using Inventario.Infraestructura.Persistencia;
using Microsoft.Data.SqlClient;

namespace Inventario.Pruebas.Infraestructura;

[Collection("BaseDatos")]
[Trait("Categoria", "Integracion")]
public sealed class EsquemaPruebas(BaseDatosFixture baseDatos)
{
    [Fact]
    public async Task Migrador_AplicaCadaScriptUnaSolaVezYEnOrden()
    {
        new MigradorEsquema(baseDatos.Opciones).Ejecutar();

        await using var conexion = await baseDatos.Fabrica.AbrirAsync(default);
        var scripts = (await conexion.QueryAsync<string>(
            "SELECT ScriptName FROM dbo.VersionesEsquema ORDER BY Id")).AsList();

        Assert.Equal(3, scripts.Count);
        Assert.EndsWith("001_crear_tablas.sql", scripts[0]);
        Assert.EndsWith("002_indices.sql", scripts[1]);
        Assert.EndsWith("003_indices_listado.sql", scripts[2]);
    }

    [Theory]
    [InlineData("Precio", "decimal", 18, 2)]
    [InlineData("FechaIngreso", "date", null, null)]
    public async Task Columnas_UsanElTipoEsperado(string columna, string tipo, int? precision, int? escala)
    {
        await using var conexion = await baseDatos.Fabrica.AbrirAsync(default);

        var real = await conexion.QuerySingleAsync<(string, int?, int?)>("""
            SELECT DATA_TYPE, CAST(NUMERIC_PRECISION AS INT), CAST(NUMERIC_SCALE AS INT)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'LotesProducto' AND COLUMN_NAME = @columna
            """, new { columna });

        Assert.Equal((tipo, precision, escala), real);
    }

    [Theory]
    [InlineData("UX_Productos_Codigo", true)]
    [InlineData("UX_LotesProducto_ProductoId_NumeroLote", true)]
    [InlineData("IX_LotesProducto_FechaIngreso", false)]
    [InlineData("IX_Productos_Activo_Nombre", false)]
    [InlineData("IX_Productos_Categoria_Activo_Nombre", false)]
    [InlineData("IX_Productos_Activo_Categoria", false)]
    public async Task Indices_Existen(string indice, bool unico)
    {
        await using var conexion = await baseDatos.Fabrica.AbrirAsync(default);

        Assert.Equal(unico, await conexion.QuerySingleAsync<bool>(
            "SELECT is_unique FROM sys.indexes WHERE name = @indice", new { indice }));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, -1)]
    public async Task Lotes_RechazanValoresInvalidos(int precio, int cantidad)
    {
        await using var conexion = await baseDatos.Fabrica.AbrirAsync(default);

        var error = await Assert.ThrowsAsync<SqlException>(() => conexion.ExecuteAsync("""
            INSERT INTO dbo.Productos (Codigo, Nombre, Descripcion, Categoria) VALUES (@codigo, 'A', '', 'A');
            INSERT INTO dbo.LotesProducto (ProductoId, NumeroLote, FechaIngreso, Precio, Cantidad)
            VALUES (SCOPE_IDENTITY(), 'L-1', '2026-01-01', @precio, @cantidad);
            """, new { codigo = Guid.NewGuid().ToString("N")[..12], precio, cantidad }));

        Assert.Contains("CK_LotesProducto", error.Message);
    }
}
