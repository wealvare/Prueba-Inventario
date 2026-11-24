# Decisiones

## Arquitectura hexagonal en el backend

El dominio no depende de ningún otro proyecto. Aplicación depende de Dominio, Infraestructura depende de Aplicación y Dominio, y Api compone la solución.

## .NET 10

Es la versión LTS vigente.

## Secretos fuera del repositorio

La contraseña de SQL Server se define en `.env`, que no se versiona. `.env.example` documenta las variables requeridas.

## Eliminación lógica de productos

Eliminar un producto lo marca como inactivo para conservar su historial de lotes. El código sigue reservado y el producto puede reactivarse al actualizarlo.

## Lotes gestionados desde el producto

Producto es la raíz del agregado. Al actualizarlo se envía la lista completa de lotes: los que tienen id se modifican, los que no lo tienen se agregan y los ausentes se eliminan. El número de lote no se repite dentro de un mismo producto.

## Código único

El código se normaliza a mayúsculas y el caso de uso comprueba que no exista antes de guardar.
