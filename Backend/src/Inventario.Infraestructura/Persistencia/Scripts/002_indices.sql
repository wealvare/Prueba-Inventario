CREATE UNIQUE INDEX UX_Usuarios_NombreUsuario ON dbo.Usuarios (NombreUsuario);
CREATE UNIQUE INDEX UX_Usuarios_Correo ON dbo.Usuarios (Correo);

CREATE UNIQUE INDEX UX_Productos_Codigo ON dbo.Productos (Codigo);
CREATE INDEX IX_Productos_Activo_Nombre ON dbo.Productos (Activo, Nombre);
CREATE INDEX IX_Productos_Categoria ON dbo.Productos (Categoria);

CREATE UNIQUE INDEX UX_LotesProducto_ProductoId_NumeroLote ON dbo.LotesProducto (ProductoId, NumeroLote);
CREATE INDEX IX_LotesProducto_FechaIngreso ON dbo.LotesProducto (FechaIngreso);
