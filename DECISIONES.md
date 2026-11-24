# Decisiones

## Arquitectura hexagonal

La elegí para que las reglas del inventario no dependan de la base de datos ni de HTTP. Así pude probar el dominio y los casos de uso sin infraestructura, y cambiar de herramienta de persistencia sin tocarlos.

| Concepto | Carpeta |
|---|---|
| Dominio | `Backend/src/Inventario.Dominio`: entidades, reglas y excepciones |
| Casos de uso | `Inventario.Aplicacion/*/CasosUso` |
| Puertos de entrada | `Inventario.Aplicacion/*/Puertos/Entrada`: lo que la Api puede pedir |
| Puertos de salida | `Inventario.Aplicacion/*/Puertos/Salida`: lo que la aplicación necesita |
| Adaptadores de salida | `Inventario.Infraestructura`: Dapper, JWT y BCrypt |
| Adaptador de entrada | `Inventario.Api`: controllers que reciben, llaman al puerto y eligen el código HTTP |

Dominio no depende de nadie, Aplicación depende de Dominio, Infraestructura de ambos, y Api compone todo. Cada capa registra sus dependencias con un método de extensión.

Usé .NET 10 por ser la versión LTS vigente.

## Producto con varios lotes

Un mismo producto entra al inventario en momentos y a precios distintos, por eso precio, cantidad y fecha pertenecen al lote y no al producto.

- Producto es la raíz del agregado: los lotes solo se modifican a través de él, y su número no se repite dentro del producto.
- Al actualizar se envía la lista completa. Los lotes con id se modifican, los que no lo tienen se agregan y los ausentes se eliminan, todo en una transacción.
- Si llega un lote que no pertenece al producto o que otra persona eliminó, revierto todo y respondo 409.
- Eliminar un producto lo marca como inactivo para conservar sus lotes. Su código sigue reservado y puede reactivarse.
- El código se normaliza a mayúsculas. El caso de uso comprueba que no exista y el índice único es la garantía final ante peticiones simultáneas.

## Dapper y versionado del esquema

Elegí Dapper porque las consultas son pocas y conocidas, y me permite controlar el SQL exacto. Todos los valores viajan como parámetros; los filtros dinámicos solo combinan fragmentos de SQL fijos.

Los repositorios reconstruyen las entidades con los métodos estáticos `Reconstruir`, sin abrir los setters del dominio.

El esquema vive en scripts numerados en `Persistencia/Scripts`. DbUp aplica los pendientes en orden, cada uno en su transacción, y los registra en `dbo.VersionesEsquema`. Un script aplicado no se edita: se agrega otro. En desarrollo se aplican al arrancar; en otros entornos la Api no modifica la base de datos.

La semilla crea los usuarios que falten y carga 15 productos solo si la tabla está vacía, en una transacción con bloqueo para que dos arranques simultáneos no dupliquen datos.

## Seguridad

- **Secretos:** están en `.env`, que no se versiona. `.env.example` documenta las variables.
- **Contraseñas:** BCrypt con factor 12 y prehash SHA-384, para no truncar a 72 bytes. Si el usuario no existe verifico un hash ficticio, de modo que el tiempo de respuesta no lo revele.
- **JWT:** valido firma, algoritmo, emisor, audiencia y expiración, con 30 segundos de tolerancia. Un cambio de rol se aplica al emitir el siguiente token.
- **Roles:** Administrador lee y escribe; Consulta solo lee. La Api lo comprueba en cada petición. En el frontend oculto botones y protejo rutas solo como ayuda de interfaz.
- **Límite de inicio de sesión:** por IP y minuto, en memoria de cada instancia.
- **Sesión:** guardo el token en `sessionStorage`, que se borra al cerrar la pestaña, y solo lo adjunto a peticiones de la Api.

Preferí `sessionStorage` a una cookie `HttpOnly` porque la Api es sin estado y recibe el token en `Authorization`, lo que evita la defensa contra CSRF y simplifica el uso entre orígenes. El costo es que un script malicioso (XSS) podría leerlo. Lo mitigo con el escape automático de Angular, sin insertar HTML, y una expiración configurable (60 minutos en el ejemplo). Una cookie `HttpOnly`, `Secure` y `SameSite` impediría leerlo, pero exige emitirla e invalidarla en la Api y protegerse de CSRF.

## CORS

Solo acepto los orígenes de `Cors__OrigenesPermitidos__*` y los métodos GET, POST, PUT y DELETE. Expongo `Location`, `Retry-After` y `X-Correlacion` para que el frontend pueda leerlos. No permito credenciales, porque no uso cookies.

En producción el frontend llama a `/api`, relativa al sitio: con un proxy en el mismo dominio no haría falta CORS.

## Optimización de consultas

El listado elige primero la página y después resume los lotes solo de esos productos. El script `003` agrega índices que cubren los filtros por estado y categoría, la lista de categorías y el resumen de lotes.

Medí lecturas lógicas con `STATISTICS IO`, con 20.000 productos y 100.000 lotes en SQL Server 2022, sobre consultas equivalentes a las de la Api:

| Consulta | Antes | Después |
|---|---|---|
| Activos, página 1 | 158 | 33 |
| Activos, página 1000 | 151.000 | 515 |
| Por categoría, activos | 292 | 33 |
| Búsqueda de texto | 42.112 | 690 |
| Rango de precios | 800 | 685 |
| Lista de categorías | 198 | 89 |

La búsqueda redujo lecturas, pero no tiempo: pasó de unos 500 a unos 470 ms, porque `CHARINDEX` evalúa cada fila. Usé `CHARINDEX` para que `%` y `_` se busquen como texto.

Incluir la descripción en `IX_Productos_Activo_Nombre` evita volver a la tabla al buscar, pero la duplica: hasta 1000 bytes más por producto, más memoria de caché y una escritura adicional al cambiarla. Lo acepto porque se lee más de lo que se escribe.

También comprimo las respuestas HTTP, pero no HTTPS, para no exponerlas a ataques como BREACH. El frontend descarga cada pantalla al visitarla y cancela la búsqueda anterior al cambiar los filtros.

## Errores y registro

**Backend.** Un middleware convierte las excepciones en ProblemDetails en español con un identificador de correlación: validación y reglas a 400, credenciales a 401, inexistente a 404, duplicados y lotes que ya no existen a 409, y el resto a 500 sin detalles internos. Serilog escribe en consola y en archivos diarios con retención de 14. Los 500 quedan como error y los 4xx como advertencia, con método, ruta, estado y duración, nunca con cuerpos, tokens ni cadenas de consulta.

**Frontend.** Un interceptor traduce cada fallo a un mensaje en español. Cada petición declara qué errores muestra su pantalla; el resto aparece como notificación. Un 401 cierra la sesión y un 500 muestra la correlación para buscarla en los registros. Los errores ajenos a la Api llegan a un `ErrorHandler` propio.

El registro del frontend queda solo en la consola del navegador. Como mejora, enviaría los errores a un endpoint propio o a un servicio de monitoreo, con límite de envíos y sin datos sensibles.

## Frontend

- El estado de sesión y las notificaciones viven en servicios con signals de solo lectura. No usé una librería de estado porque no hay estado complejo que compartir.
- La URL es la única fuente de los filtros, así sobreviven a recargas y enlaces compartidos.
- Las validaciones repiten las del backend para responder de inmediato, pero la autoridad es la Api.
- Los precios se muestran en dólares con formato es-EC. Es solo presentación: la Api no guarda la moneda.
- Los errores de campo se anuncian con `aria-live`, los controles miden al menos 44 px y el diálogo mantiene el foco dentro. El listado usa tarjetas por debajo de 1024 px.

## Patrones

| Patrón | Dónde |
|---|---|
| Puertos y adaptadores | Interfaces de `Puertos` y sus implementaciones |
| Repositorio | `RepositorioProductos`, `RepositorioUsuarios` |
| Caso de uso | Una clase por operación en `CasosUso` |
| Agregado | `Producto` con sus `LoteProducto` |
| DTO y mapeo | `Dtos` y `MapeoProducto` |
| Fábrica | `IFabricaConexion` |
| Inyección de dependencias | `AgregarAplicacion`, `AgregarInfraestructura`, `AgregarApi` |
| Middleware | Errores y correlación |
| Interceptor y guardia | `nucleo/interceptores`, `nucleo/guardias` |

## Desafíos

- **Paginación costosa:** al medir vi que el listado resumía los lotes de todas las filas anteriores a la página. Lo resolví paginando antes de resumir.
- **Duplicados simultáneos:** comprobar el código antes de guardar no basta. Identifico el índice infringido en los errores 2601 y 2627 y lo traduzco a la excepción de dominio.
- **Intercambiar números entre lotes:** actualizarlos uno por uno violaría el índice único a mitad de camino. Los actualizo en una sola sentencia con `OPENJSON`.
- **Filtros y URL:** el formulario y la URL se actualizaban entre sí. Dejé la URL como fuente y el formulario solo navega.
- **Foco del diálogo:** al cerrarlo, el foco se perdía. Guardo el elemento activo al abrir y se lo devuelvo.
- **Respuestas con JSON inválido:** Angular las informa como fallo con estado 200. Las trato como error del servidor.
- **Serilog:** la versión 11 de `Serilog.AspNetCore` arrastraba dependencias de .NET 11. Fijé la 10.

## Limitaciones y mejoras

- **Edición simultánea:** gana la última persona en guardar, salvo duplicados o lotes eliminados. Agregaría una columna `rowversion`.
- **Búsqueda:** recorre un índice completo, igual que el listado de todos los estados ordena todas sus filas. Con más volumen usaría búsqueda de texto completo.
- **Límite de inicio de sesión:** con varias instancias requiere un contador compartido, y detrás de un proxy, los encabezados reenviados.
- **Sesión:** no hay renovación ni revocación de tokens.
- **Usuarios:** solo existen los de la semilla; no hay pantalla para administrarlos.
- **Listado:** no permite elegir el orden.
- **Formulario:** no avisa al salir con cambios sin guardar. El precio llega a 9.999.999.999.999,99 porque por encima JavaScript pierde centavos.
- **Despliegue:** falta configurar el proxy de `/api` y un paso que aplique los scripts.

## Dónde tocar

Abreviaturas: `DOM`, `APL`, `INF` y `API` son los proyectos `Backend/src/Inventario.*`; `PRU` es `Backend/tests/Inventario.Pruebas`; `PROD` es `Frontend/src/app/funcionalidades/productos`; `NUC` es `Frontend/src/app/nucleo`.

### Agregar un campo a Producto

1. `INF/Persistencia/Scripts/004_nombre.sql` (nuevo): `ALTER TABLE dbo.Productos ADD ...`. Si es obligatorio, con `DEFAULT` para las filas existentes. Se incrusta solo.
2. `DOM/Productos/Producto.cs`: constante de longitud, propiedad, constructor, `Reconstruir` y `Actualizar`.
3. `APL/Productos/Dtos`: `ProductoDto.cs`, `SolicitudCrearProducto.cs` y `SolicitudActualizarProducto.cs`; `ProductoResumenDto.cs` si va en el listado.
4. `APL/Productos/Mapeos/MapeoProducto.cs`.
5. `APL/Productos/CasosUso`: `CrearProducto.cs` y `ActualizarProducto.cs`.
6. `APL/Productos/Validadores`: regla en `ReglasProducto.cs`, usada en `ValidadorCrearProducto.cs` y `ValidadorActualizarProducto.cs`.
7. `INF/Persistencia/Productos/SqlProductos.cs`: `ObtenerConLotes`, `Insertar` y `Actualizar`. Para el listado, `Listar` en la CTE y en el SELECT final; revisa si un índice debe incluir la columna.
8. `INF/Persistencia/Productos/FilaProducto.cs`, en el mismo orden que el SELECT, y `Datos` en `RepositorioProductos.cs`.
9. `INF/Persistencia/Semilla/CatalogoSemilla.cs`.
10. `API/Documentacion/FiltroEjemplos.cs`. El controller no cambia.
11. `PROD/modelos/producto.ts`.
12. `PROD/pagina-formulario-producto/formulario-producto.ts` (tipo, `crearFormulario`, `cargarProducto` y `leerSolicitud`) y `pagina-formulario-producto.html`.
13. `PROD/pagina-detalle-producto/pagina-detalle-producto.html`. Para el listado, la tabla y las tarjetas de `PROD/pagina-productos/pagina-productos.html`.
14. Pruebas: `PRU/Dominio/ProductoPruebas.cs`, `PRU/Aplicacion/ProductosPruebas.cs`, `PRU/Infraestructura/RepositorioProductosPruebas.cs`, `PRU/Infraestructura/EsquemaPruebas.cs` (cantidad de scripts), `PRU/Api/ProductosApiPruebas.cs`, `Frontend/src/app/pruebas/productos-prueba.ts` y los `.spec.ts` de `PROD`.

### Agregar un filtro al listado

1. `APL/Productos/Dtos/FiltroProductos.cs`: parámetro opcional con valor por defecto.
2. `APL/Productos/Validadores/ValidadorFiltroProductos.cs`: regla y mensaje.
3. `INF/Persistencia/Productos/CondicionesProductos.cs`: fragmento fijo y parámetro. Si filtra por una columna nueva, un script con su índice.
4. `API/Documentacion/FiltroParametros.cs`: descripción en Swagger.
5. `PROD/modelos/producto.ts`: `FiltroProductos`.
6. `PROD/pagina-productos/filtro-url.ts`: `FILTRO_INICIAL` y `leerFiltro`.
7. `PROD/pagina-productos/pagina-productos.ts`: control en `formulario` y campo en `leerOrden`, en el mismo orden que devuelve `leerFiltro`.
8. `PROD/pagina-productos/pagina-productos.html`: control dentro de `app-campo`.
9. Pruebas: `PRU/Aplicacion/ProductosPruebas.cs`, `PRU/Infraestructura/RepositorioProductosPruebas.cs`, `PRU/Api/ProductosApiPruebas.cs`, `filtro-url.spec.ts` y `pagina-productos.spec.ts`.

`servicio-productos.ts` no cambia: envía todos los filtros con valor.

### Cambiar una regla de validación

1. `DOM/Productos/Producto.cs` o `LoteProducto.cs`: la regla y su mensaje. Las longitudes son constantes que los validadores reutilizan.
2. `APL/Productos/Validadores/ReglasProducto.cs` (producto) o `ValidadorLote.cs` (lote).
3. Si cambia una longitud, precisión o `CHECK`, un script nuevo en `INF/Persistencia/Scripts`. Si una columna está en un índice, puede que haya que recrearlo.
4. `PROD/pagina-formulario-producto/formulario-producto.ts`: validadores del control. Si la regla es nueva, `Frontend/src/app/compartido/formularios/validadores.ts` y su texto en `mensaje-error.ts`.
5. Pruebas: `PRU/Dominio/ProductoPruebas.cs`, `PRU/Aplicacion/ProductosPruebas.cs` y `formulario-producto.spec.ts`.

### Agregar un rol

1. `DOM/Usuarios/RolUsuario.cs`.
2. Script nuevo: reemplazar `CK_Usuarios_Rol` con el rol agregado.
3. `INF/Configuracion/OpcionesSemilla.cs`, `.env.example`, `.env` y los valores de prueba en `PRU/Api/FabricaApi.cs` y `PRU/Infraestructura/OpcionesPruebas.cs`, si tendrá usuario inicial.
4. `API/Productos/ProductosController.cs`: `[Authorize(Roles = ...)]` de cada acción. Un rol nuevo ya puede leer, porque el controller solo exige sesión.
5. `NUC/modelos/sesion.ts` (`Rol`) y `NUC/servicios/lector-token.ts` (`ROLES`); si falta aquí, el frontend rechaza el token.
6. `NUC/servicios/servicio-autenticacion.ts`: señal como `esAdministrador`. Si protege rutas, una guardia en `NUC/guardias` y su uso en `PROD/rutas-productos.ts`.
7. Pruebas: `PRU/Dominio/UsuarioPruebas.cs`, `PRU/Infraestructura/EsquemaPruebas.cs`, `PRU/Infraestructura/SemillaPruebas.cs`, `PRU/Api/ProductosApiPruebas.cs`, `NUC/guardias/guardias.spec.ts` y `NUC/servicios/servicio-autenticacion.spec.ts`.

El token ya lleva el rol, así que `ServicioToken` no cambia.

### Restringir un endpoint

1. `API/Productos/ProductosController.cs`: `[Authorize(Roles = SoloAdministrador)]` y `[SwaggerResponse(403, ...)]` en la acción.
2. `PROD/rutas-productos.ts`: `canActivate: [guardiaAdministrador]` en su ruta, y `@if (esAdministrador())` en los enlaces que llevan a ella.
3. Pruebas: `Consulta_AlEscribir_Recibe403` en `PRU/Api/ProductosApiPruebas.cs` y `PROD/rutas-productos.spec.ts`. Actualiza la tabla de endpoints de `README.md`.

### Cambiar el tamaño de página

1. `APL/Productos/Dtos/FiltroProductos.cs`: valor por defecto de la Api.
2. `APL/Productos/Validadores/ValidadorFiltroProductos.cs`: `TamanoMaximo`, y el texto en `API/Documentacion/FiltroParametros.cs`.
3. `PROD/pagina-productos/filtro-url.ts`: `FILTRO_INICIAL.tamanoPagina` y `TAMANOS_PAGINA`, que debe contener el valor inicial.
4. Pruebas: `Listar_AplicaValoresPorDefecto` en `PRU/Api/ProductosApiPruebas.cs`, `PRU/Aplicacion/ProductosPruebas.cs`, `filtro-url.spec.ts` y `pagina-productos.spec.ts`.

### Cambiar el formato de moneda

1. `Frontend/src/app/app.config.ts`: datos regionales importados, `LOCALE_ID` y `DEFAULT_CURRENCY_CODE`.
2. `PROD/pagina-formulario-producto/pagina-formulario-producto.html`: etiqueta `Precio (USD)`.
3. Pruebas: `pagina-productos.spec.ts` y `pagina-detalle-producto.spec.ts`.

### Ejecutar solo las pruebas afectadas

Desde `Backend`. Las de `Infraestructura` que usan base de datos y las de `Api` requieren Docker:

```bash
dotnet test --filter "FullyQualifiedName~Dominio.ProductoPruebas"
dotnet test --filter "FullyQualifiedName~Aplicacion.ProductosPruebas"
dotnet test --filter "FullyQualifiedName~RepositorioProductosPruebas"
dotnet test --filter "FullyQualifiedName~EsquemaPruebas|FullyQualifiedName~SemillaPruebas"
dotnet test --filter "FullyQualifiedName~ProductosApiPruebas"
```

Desde `Frontend`:

```bash
npm test -- --watch=false --include="src/app/funcionalidades/productos/**/*.spec.ts"
npm test -- --watch=false --include="src/app/nucleo/**/*.spec.ts"
```
