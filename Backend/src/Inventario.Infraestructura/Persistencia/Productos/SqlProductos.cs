namespace Inventario.Infraestructura.Persistencia.Productos;

internal static class SqlProductos
{
    public const string ObtenerConLotes = """
        SELECT Id, Codigo, Nombre, Descripcion, Categoria, Activo
        FROM dbo.Productos
        WHERE Id = @id;

        SELECT Id, NumeroLote, FechaIngreso, Precio, Cantidad
        FROM dbo.LotesProducto
        WHERE ProductoId = @id
        ORDER BY FechaIngreso, Id;
        """;

    public const string ExisteCodigo = """
        SELECT CAST(CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Productos WHERE Codigo = @codigo AND (@idExcluido IS NULL OR Id <> @idExcluido)
        ) THEN 1 ELSE 0 END AS BIT);
        """;

    public const string Insertar = """
        INSERT INTO dbo.Productos (Codigo, Nombre, Descripcion, Categoria, Activo)
        OUTPUT INSERTED.Id
        VALUES (@Codigo, @Nombre, @Descripcion, @Categoria, @Activo);
        """;

    public const string Actualizar = """
        UPDATE dbo.Productos
        SET Codigo = @Codigo, Nombre = @Nombre, Descripcion = @Descripcion, Categoria = @Categoria, Activo = @Activo
        WHERE Id = @Id;
        """;

    public const string InsertarLote = """
        INSERT INTO dbo.LotesProducto (ProductoId, NumeroLote, FechaIngreso, Precio, Cantidad)
        VALUES (@productoId, @NumeroLote, @fechaIngreso, @Precio, @Cantidad);
        """;

    public const string ActualizarLotes = """
        UPDATE lote
        SET NumeroLote = datos.NumeroLote,
            FechaIngreso = datos.FechaIngreso,
            Precio = datos.Precio,
            Cantidad = datos.Cantidad
        FROM dbo.LotesProducto AS lote
        INNER JOIN OPENJSON(@lotes) WITH (
            Id INT,
            NumeroLote NVARCHAR(50),
            FechaIngreso DATE,
            Precio DECIMAL(18, 2),
            Cantidad INT
        ) AS datos ON datos.Id = lote.Id
        WHERE lote.ProductoId = @productoId;
        """;

    public const string EliminarLotes = """
        DELETE FROM dbo.LotesProducto
        WHERE ProductoId = @productoId AND Id IN (SELECT CAST(value AS INT) FROM OPENJSON(@ids));
        """;

    public static string Listar(string condiciones) => $"""
        SELECT COUNT(*)
        FROM dbo.Productos AS producto
        WHERE {condiciones};

        SELECT producto.Id, producto.Codigo, producto.Nombre, producto.Categoria, producto.Activo,
            resumen.CantidadLotes, resumen.CantidadTotal, resumen.PrecioMinimo, resumen.PrecioMaximo
        FROM dbo.Productos AS producto
        OUTER APPLY (
            SELECT COUNT(*) AS CantidadLotes,
                COALESCE(SUM(CAST(lote.Cantidad AS BIGINT)), 0) AS CantidadTotal,
                MIN(lote.Precio) AS PrecioMinimo,
                MAX(lote.Precio) AS PrecioMaximo
            FROM dbo.LotesProducto AS lote
            WHERE lote.ProductoId = producto.Id
        ) AS resumen
        WHERE {condiciones}
        ORDER BY producto.Nombre, producto.Id
        OFFSET @desplazamiento ROWS FETCH NEXT @tamanoPagina ROWS ONLY;
        """;
}
