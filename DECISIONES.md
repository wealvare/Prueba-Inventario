# Decisiones

## Arquitectura hexagonal en el backend

El dominio no depende de ningún otro proyecto. Aplicación depende de Dominio, Infraestructura depende de Aplicación y Dominio, y Api compone la solución.

## .NET 10

Es la versión LTS vigente.

## Secretos fuera del repositorio

La contraseña de SQL Server se define en `.env`, que no se versiona. `.env.example` documenta las variables requeridas.
