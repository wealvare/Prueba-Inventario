namespace Inventario.Infraestructura.Persistencia.Usuarios;

internal static class SqlUsuarios
{
    public const string ObtenerPorNombre = """
        SELECT Id, NombreUsuario, Correo, HashContrasena, Rol, Activo
        FROM dbo.Usuarios
        WHERE NombreUsuario = @nombreUsuario;
        """;

    public const string ObtenerPorId = """
        SELECT Id, NombreUsuario, Correo, HashContrasena, Rol, Activo
        FROM dbo.Usuarios
        WHERE Id = @id;
        """;

    public const string ExisteNombre = """
        SELECT CAST(CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Usuarios
            WHERE NombreUsuario = @nombreUsuario AND (@idExcluido IS NULL OR Id <> @idExcluido)
        ) THEN 1 ELSE 0 END AS BIT);
        """;

    public const string ExisteCorreo = """
        SELECT CAST(CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Usuarios WHERE Correo = @correo AND (@idExcluido IS NULL OR Id <> @idExcluido)
        ) THEN 1 ELSE 0 END AS BIT);
        """;

    public const string ExisteOtroAdministrador = """
        SELECT CAST(CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Usuarios WHERE Rol = N'Administrador' AND Activo = 1 AND Id <> @id
        ) THEN 1 ELSE 0 END AS BIT);
        """;

    public const string ExisteOtroAdministradorConBloqueo = """
        SELECT CAST(CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Usuarios WITH (TABLOCKX, HOLDLOCK)
            WHERE Rol = N'Administrador' AND Activo = 1 AND Id <> @id
        ) THEN 1 ELSE 0 END AS BIT);
        """;

    public const string Listar = """
        SELECT COUNT(*)
        FROM dbo.Usuarios;

        SELECT Id, NombreUsuario, Correo, Rol, Activo
        FROM dbo.Usuarios
        ORDER BY NombreUsuario
        OFFSET @desplazamiento ROWS FETCH NEXT @tamanoPagina ROWS ONLY;
        """;

    public const string Insertar = """
        INSERT INTO dbo.Usuarios (NombreUsuario, Correo, HashContrasena, Rol, Activo)
        OUTPUT INSERTED.Id
        VALUES (@NombreUsuario, @Correo, @HashContrasena, @rol, @Activo);
        """;

    public const string Actualizar = """
        UPDATE dbo.Usuarios
        SET NombreUsuario = @NombreUsuario, Correo = @Correo, HashContrasena = @HashContrasena, Rol = @rol, Activo = @Activo
        WHERE Id = @Id;
        """;
}
