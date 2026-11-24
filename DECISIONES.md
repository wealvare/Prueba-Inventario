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

Las consultas del inventario son pocas y conocidas, y Dapper permite escribir el SQL exacto: paginación con `OFFSET/FETCH`, resumen de lotes y producto con sus lotes en un solo viaje. Cada escritura de un producto y sus lotes ocurre en una transacción, y el producto informa qué lotes se quitaron.

Todos los valores viajan como parámetros. Los filtros dinámicos solo combinan fragmentos de SQL fijos.

Los repositorios reconstruyen las entidades leídas con los métodos estáticos `Reconstruir` del dominio. Así los constructores y setters siguen protegidos, la intención queda explícita y no hace falta acceder a miembros privados desde Infraestructura. Al guardar, el repositorio devuelve el producto leído de nuevo, con los ids asignados por la base de datos.

## Esquema versionado con DbUp

El esquema vive en scripts SQL numerados, embebidos en Infraestructura. DbUp aplica los pendientes en orden, cada uno en su transacción, y los registra en `dbo.VersionesEsquema`. Un script aplicado no se edita: cada cambio se agrega con el siguiente número.

En desarrollo, la Api aplica los scripts y carga los datos iniciales al arrancar. En otros entornos no modifica la base de datos.

## Sincronización de lotes

Dentro de la transacción se eliminan los lotes ausentes, se actualizan los existentes por id y se insertan los nuevos. La actualización es una sola sentencia, por lo que se pueden intercambiar números entre lotes. Si se envía un lote que no pertenece al producto o que otro usuario eliminó, se revierte todo y se responde 409 para que el cliente recargue el producto.

## Violaciones de unicidad

Los índices únicos son la garantía final ante peticiones simultáneas. El repositorio identifica el índice infringido en los errores 2601 y 2627 y lanza la excepción de dominio correspondiente.

## Estado en el listado

El filtro `estado` admite activos, inactivos y todos. Si no se indica, se listan los activos.

## Contraseñas y tokens

Las contraseñas se guardan con BCrypt, factor 12 y prehash SHA-384 para no truncar a 72 bytes. El token incluye el rol, y su clave, emisor, audiencia y expiración provienen de la configuración. Al iniciar sesión siempre se verifica un hash: si el usuario no existe, uno ficticio con el mismo coste, para que los tiempos de respuesta no revelen qué usuarios existen.

## Datos iniciales

Los usuarios se crean si no existen, con contraseñas tomadas de la configuración. Los 15 productos se cargan solo si la tabla está vacía. Todo ocurre en una transacción con bloqueo de tabla, así que un fallo no deja datos parciales y dos arranques simultáneos no los duplican.

## Edición simultánea

Si dos personas editan el mismo producto, gana la última en guardar. Se eligió así porque el inventario lo administran pocas personas, los choques son poco probables y evita exigir una versión del registro en cada petición y resolver conflictos en la interfaz. La base de datos sigue impidiendo códigos y lotes duplicados, y se rechaza actualizar un lote que otra persona eliminó. Si el uso concurrente creciera, se añadiría una columna `rowversion`.

## Capa Api

Los controllers solo reciben la petición, llaman al puerto de entrada y eligen el código HTTP. Cada capa registra sus dependencias con un método de extensión: `AgregarAplicacion`, `AgregarInfraestructura` y `AgregarApi`.

## Errores

Todas las respuestas de error usan ProblemDetails en español, con el identificador de correlación. Un middleware traduce las excepciones: validación y reglas de negocio a 400, credenciales a 401, producto inexistente a 404, duplicados y lotes que ya no existen a 409 y el resto a 500 sin detalles internos. Los 500 se registran como error, y toda petición rechazada deja una advertencia con método, ruta, estado y correlación. Los datos mal formados registran además los campos afectados, nunca sus valores.

## Autorización

El rol viaja en el token. Administrador puede leer y escribir, y Consulta solo leer. El token se valida por firma, algoritmo, emisor, audiencia y expiración, con 30 segundos de tolerancia de reloj. Un cambio de rol se aplica al emitir el siguiente token.

## Límite de inicio de sesión

Se limita por dirección IP en ventanas de un minuto, en memoria de cada instancia. Detrás de un proxy habría que configurar los encabezados reenviados, y con varias instancias, un contador compartido.

## Registros

Serilog escribe en consola y en archivos diarios con retención de 14. Se registran método, ruta, estado y duración, nunca cuerpos, encabezados de autorización ni cadenas de consulta. El encabezado `X-Correlacion` se acepta solo con letras, números y guiones; en otro caso se genera uno nuevo.

## Compresión

Se aplica solo sobre HTTP y queda desactivada en HTTPS para no exponer las respuestas a ataques como BREACH. En producción conviene delegarla en el proxy.

## Swagger

Se publica solo en desarrollo. La estructura se documenta con atributos y los ejemplos con un filtro, sin comentarios XML en el código.

## Índices del listado

El listado elige primero la página y después resume los lotes solo de esos productos, así su coste no crece con el número de página. `IX_Productos_Activo_Nombre` cubre el filtro por estado, el orden y la búsqueda; `IX_Productos_Categoria_Activo_Nombre`, el filtro por categoría; `IX_Productos_Activo_Categoria`, la lista de categorías; y el índice único de lotes incluye precio y cantidad para el resumen y el rango de precios.

La búsqueda por texto contenido y el listado de todos los estados recorren un índice completo. Es suficiente para este volumen; con mucho más convendría búsqueda de texto completo.

## Frontend con signals

El estado de sesión y las notificaciones viven en servicios con signals de solo lectura hacia fuera, y los valores derivados se calculan con `computed`. RxJS queda para las peticiones HTTP. No se usa una librería de estado porque no hay estado complejo que compartir.

## Carga diferida

Cada funcionalidad declara sus rutas y sus páginas se descargan al visitarlas. El diseño principal también, de modo que el inicio de sesión no lo carga. El código se organiza en `nucleo`, `compartido` y `funcionalidades`.

## Sesión en el navegador

El token se guarda en `sessionStorage`: sobrevive a recargas, pero se borra al cerrar la pestaña. El frontend lee el rol y la expiración solo para adaptar la interfaz; la autorización real la hace la Api. La sesión se cierra con un temporizador y también se comprueba la vigencia antes de cada petición, por si el equipo estuvo suspendido. La ruta de retorno tras iniciar sesión solo admite rutas internas.

Se eligió `sessionStorage` en lugar de una cookie `HttpOnly` porque la Api es sin estado y recibe el token en el encabezado `Authorization`, lo que evita la protección contra CSRF y simplifica el uso entre orígenes distintos. A cambio, un script malicioso (XSS) podría leer el token. Se mitiga con el escape automático de Angular, sin insertar HTML, y con una expiración corta. Una cookie `HttpOnly`, `Secure` y `SameSite` impediría leerlo, pero exige que la Api la emita y la invalide, defensa contra CSRF y, en lo posible, un mismo dominio.

## URL de la Api en producción

La compilación de producción usa `/api`, relativa al sitio, para no fijar una dirección en el código. Al desplegar hay que configurar el proxy correspondiente o indicar la dirección completa.

## Errores en el frontend

Un interceptor convierte las respuestas fallidas en `ErrorApi`, con mensaje en español, correlación y errores por campo. Usa el detalle de ProblemDetails en los errores de negocio y un texto genérico en los de servidor, donde además muestra la correlación para poder buscarla en los registros. El 401 cierra la sesión, salvo en el propio inicio de sesión. Una respuesta correcta con JSON inválido se trata como error del servidor. Los errores ajenos a la Api, como no poder descargar una pantalla, llegan a un `ErrorHandler` propio que los registra y avisa. El registro del frontend escribe en la consola método, URL, estado y correlación, sin cuerpos ni tokens.

El token solo se adjunta a peticiones dirigidas a la Api.

## Filtros del listado en la URL

La URL es la única fuente del filtro: el formulario navega tras 400 ms sin cambios y el listado se consulta al cambiar los parámetros. Así la búsqueda sobrevive a recargas y al botón atrás. `rxResource` cancela la petición anterior cuando el filtro cambia. Solo se usan los parámetros que admite la Api, por eso no hay ordenamiento. Las categorías del desplegable son las de los productos activos; si la URL trae otra, se agrega a las opciones para no perder el filtro.

## Formulario de producto

Crear y editar comparten pantalla. Las validaciones reflejan las del backend para responder de inmediato, y la Api sigue siendo la autoridad: sus errores 400 se muestran junto al campo y los 409 como mensaje, sin perder lo escrito. El precio se limita a 9.999.999.999.999,99, menos que la base de datos, porque por encima un número de JavaScript pierde centavos.

## Precios en dólares

Los precios se muestran con el formato es-EC: símbolo $, dos decimales, coma decimal y punto de miles. Es solo presentación: la Api no guarda la moneda ni convierte valores.

## Accesibilidad

Los errores de cada campo se anuncian con `aria-live`, los controles miden al menos 44 px y el foco siempre es visible. El listado usa tarjetas por debajo de 1024 px y tabla a partir de ahí.

## Errores en pantalla o en notificación

Cada petición indica qué errores muestra su propia pantalla mediante un `HttpContextToken`. El resto aparece como notificación, de modo que un mismo error no se muestra dos veces.

## Confirmación de eliminación

Usa el elemento `dialog` nativo, que bloquea el resto de la página y cierra con Escape. Además mantiene el foco dentro y lo devuelve al botón que lo abrió.

## Control por rol en el frontend

Los botones de escritura se ocultan y las rutas se protegen con la guardia de administrador. Es una ayuda de interfaz: la Api vuelve a comprobar el rol en cada petición.
