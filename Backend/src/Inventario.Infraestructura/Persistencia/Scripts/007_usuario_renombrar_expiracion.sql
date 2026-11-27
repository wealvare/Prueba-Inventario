IF COL_LENGTH('dbo.Usuarios', 'ExpiraContracena') IS NOT NULL
    EXEC sp_rename 'dbo.Usuarios.ExpiraContracena', 'ExpiraContrasena', 'COLUMN';
