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

## Dapper para el acceso a datos

Las consultas del inventario son pocas y conocidas, y Dapper permite escribir el SQL exacto: paginación con `OFFSET/FETCH`, resumen de lotes y producto con sus lotes en un solo viaje. A cambio, el repositorio registra los productos nuevos y los leídos para edición y los persiste en `GuardarCambiosAsync` dentro de una transacción.

Todos los valores viajan como parámetros. Los filtros dinámicos solo combinan fragmentos de SQL fijos.

Los identificadores y los lotes se reconstruyen con `UnsafeAccessor` en Infraestructura, para no abrir los constructores ni los setters del dominio.

## Esquema versionado con DbUp

El esquema vive en scripts SQL numerados, embebidos en Infraestructura. DbUp aplica los pendientes en orden, cada uno en su transacción, y los registra en `dbo.VersionesEsquema`. Un script aplicado no se edita: cada cambio se agrega con el siguiente número.

En desarrollo, la Api aplica los scripts y carga los datos iniciales al arrancar. En otros entornos no modifica la base de datos.

## Sincronización de lotes

Dentro de la transacción se eliminan los lotes ausentes, se actualizan los existentes por id y se insertan los nuevos. La actualización es una sola sentencia, por lo que se pueden intercambiar números entre lotes. Si otro usuario eliminó un lote que se intenta actualizar, se revierte todo y se informa.

## Violaciones de unicidad

Los índices únicos son la garantía final ante peticiones simultáneas. El repositorio identifica el índice infringido en los errores 2601 y 2627 y lanza la excepción de dominio correspondiente.

## Listado de productos activos

Si no se indica el filtro `activo`, solo se listan los productos activos.

## Contraseñas y tokens

Las contraseñas se guardan con BCrypt, factor 12 y prehash SHA-384 para no truncar a 72 bytes. El token incluye el rol, y su clave, emisor, audiencia y expiración provienen de la configuración.

## Datos iniciales

Los usuarios se crean si no existen, con contraseñas tomadas de la configuración. Los 15 productos se cargan solo si la tabla está vacía.
