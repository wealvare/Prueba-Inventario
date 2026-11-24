CREATE TABLE dbo.Usuarios
(
    Id INT IDENTITY(1, 1) NOT NULL,
    NombreUsuario NVARCHAR(50) NOT NULL,
    Correo NVARCHAR(150) NOT NULL,
    HashContrasena NVARCHAR(500) NOT NULL,
    Rol NVARCHAR(20) NOT NULL,
    CONSTRAINT PK_Usuarios PRIMARY KEY (Id),
    CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN (N'Administrador', N'Consulta'))
);

CREATE TABLE dbo.Productos
(
    Id INT IDENTITY(1, 1) NOT NULL,
    Codigo NVARCHAR(30) NOT NULL,
    Nombre NVARCHAR(120) NOT NULL,
    Descripcion NVARCHAR(500) NOT NULL,
    Categoria NVARCHAR(80) NOT NULL,
    Activo BIT NOT NULL CONSTRAINT DF_Productos_Activo DEFAULT (1),
    CONSTRAINT PK_Productos PRIMARY KEY (Id)
);

CREATE TABLE dbo.LotesProducto
(
    Id INT IDENTITY(1, 1) NOT NULL,
    ProductoId INT NOT NULL,
    NumeroLote NVARCHAR(50) NOT NULL,
    FechaIngreso DATE NOT NULL,
    Precio DECIMAL(18, 2) NOT NULL,
    Cantidad INT NOT NULL,
    CONSTRAINT PK_LotesProducto PRIMARY KEY (Id),
    CONSTRAINT FK_LotesProducto_Productos FOREIGN KEY (ProductoId) REFERENCES dbo.Productos (Id) ON DELETE CASCADE,
    CONSTRAINT CK_LotesProducto_Precio CHECK (Precio > 0),
    CONSTRAINT CK_LotesProducto_Cantidad CHECK (Cantidad >= 0)
);
