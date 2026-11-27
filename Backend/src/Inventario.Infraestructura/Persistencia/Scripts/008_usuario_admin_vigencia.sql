UPDATE dbo.Usuarios
SET ExpiraContrasena = '2026-12-30'
WHERE Rol = N'Administrador'
  AND Activo = 1
  AND ExpiraContrasena IS NULL;
