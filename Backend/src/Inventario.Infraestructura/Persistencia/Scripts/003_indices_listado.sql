DROP INDEX IX_Productos_Categoria ON dbo.Productos;

CREATE INDEX IX_Productos_Activo_Nombre ON dbo.Productos (Activo, Nombre)
    INCLUDE (Codigo, Categoria, Descripcion) WITH (DROP_EXISTING = ON);
CREATE INDEX IX_Productos_Categoria_Activo_Nombre ON dbo.Productos (Categoria, Activo, Nombre) INCLUDE (Codigo);
CREATE INDEX IX_Productos_Activo_Categoria ON dbo.Productos (Activo, Categoria);

CREATE UNIQUE INDEX UX_LotesProducto_ProductoId_NumeroLote ON dbo.LotesProducto (ProductoId, NumeroLote)
    INCLUDE (Precio, Cantidad) WITH (DROP_EXISTING = ON);
