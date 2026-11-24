# Inventario de productos

Prueba técnica fullstack de Wilber Alvarez.

Aplicación para administrar productos y sus lotes. Cada producto puede tener varios lotes, con fecha de ingreso, precio y cantidad propios. El usuario Administrador gestiona el inventario y los usuarios, y el usuario Consulta solo ve el inventario.

| Parte | Tecnologías |
|---|---|
| Backend | .NET 10, arquitectura hexagonal, Dapper, JWT |
| Frontend | Angular 22.3, TypeScript 6.0, Tailwind CSS 4.3 |
| Base de datos | SQL Server 2022 CU28, en Docker |
| Pruebas | xUnit, Testcontainers, Vitest |

## Requisitos

| Herramienta | Versión mínima | Probado con | Comprobar |
|---|---|---|---|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) o motor Docker | Compose v2 | Docker 29.4.0, Compose 5.1.1 | `docker version` y `docker compose version` |
| [SDK de .NET](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0.400 | 10.0.400 | `dotnet --list-sdks` |
| [Node.js](https://nodejs.org/en/download) | 22.22.3, 24.15.0 o 26.0.0 | 24.22.1 | `node --version` |
| npm (incluido con Node.js) | 8.0.0 | 11.19.1 | `npm --version` |
| [Git](https://git-scm.com/downloads) | Sin mínimo | 2.56.1 | `git --version` |

De Node.js sirven la serie 22 desde 22.22.3, la 24 desde 24.15.0 y cualquier versión desde la 26; las series 23 y 25 no. Docker debe estar en ejecución, con al menos 2 GB de memoria disponibles para SQL Server.

También deben estar libres los puertos 4200 (frontend), 5165 (Api) y 1433 (SQL Server, se puede cambiar).

## Inicio rápido

```bash
git clone https://github.com/wealvare/Prueba-Inventario.git
cd Prueba-Inventario
```

En Windows:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/iniciar.ps1
```

En Linux o macOS:

```bash
./scripts/iniciar.sh
```

El script comprueba los requisitos, crea `.env` con secretos aleatorios si no existe, levanta SQL Server, instala las dependencias, inicia la Api y el frontend y abre el navegador. La primera vez tarda varios minutos por las descargas. Cuando termina, los servicios siguen en segundo plano y sus registros quedan en `.ejecucion`.

`-ExecutionPolicy Bypass` solo afecta a esa ejecución: no cambia la política de PowerShell del equipo.

| Windows | Linux y macOS | Efecto |
|---|---|---|
| `-SinNavegador` | `--sin-navegador` | No abre el navegador |
| `-SoloBackend` | `--solo-backend` | Inicia solo SQL Server y la Api; no requiere Node.js |
| `-ConPruebas` | `--con-pruebas` | Ejecuta las pruebas antes de iniciar |

## Direcciones

| Servicio | URL |
|---|---|
| Frontend | `http://localhost:4200` |
| Api | `http://localhost:5165/api` |
| Swagger | `http://localhost:5165/swagger` |
| Estado | `http://localhost:5165/salud` |

## Usuarios de demostración

Se crean al iniciar la Api con los valores de `.env`:

| Rol | Usuario | Contraseña |
|---|---|---|
| Administrador | `Semilla__Administrador__NombreUsuario` | `Semilla__Administrador__Contrasena` |
| Consulta | `Semilla__Consulta__NombreUsuario` | `Semilla__Consulta__Contrasena` |

Son credenciales solo para demostración. Los usuarios se crean si no existen: cambiar después `.env` no modifica los ya guardados. Para aplicarlo, reinicia con datos limpios.

## Detener y reiniciar

| Acción | Windows | Linux y macOS |
|---|---|---|
| Detener, conservando los datos | `powershell -ExecutionPolicy Bypass -File scripts/detener.ps1` | `./scripts/detener.sh` |
| Detener y borrar los datos | `powershell -ExecutionPolicy Bypass -File scripts/detener.ps1 -BorrarDatos` | `./scripts/detener.sh --borrar-datos` |

Para reiniciar con datos limpios, detén borrando los datos y ejecuta de nuevo el script de inicio.

## Inicio manual

Alternativa a los scripts, desde la raíz del repositorio. Sirve en PowerShell y en Bash.

1. Crea `.env`:

   ```bash
   cp .env.example .env
   ```

   Completa los valores vacíos:

   | Variable | Valor |
   |---|---|
   | `BaseDatos__Contrasena` | Contraseña de SQL Server: mínimo 8 caracteres con mayúsculas, minúsculas y números |
   | `Jwt__Clave` | Clave de firma de al menos 32 caracteres |

   Encierra entre comillas simples los valores con caracteres especiales. Las variables de entorno del sistema tienen prioridad sobre `.env`.

2. Inicia la base de datos y espera a que esté saludable:

   ```bash
   docker compose up -d --wait
   ```

3. Inicia la Api. Crea la base de datos, aplica los scripts pendientes y carga los datos iniciales:

   ```bash
   cd Backend
   dotnet run --project src/Inventario.Api --launch-profile http
   ```

4. En otra terminal, desde la raíz, inicia el frontend:

   ```bash
   cd Frontend
   npm ci
   npm start
   ```

Para detener, pulsa Ctrl+C en ambas terminales y ejecuta `docker compose down` en la raíz. Con `docker compose down -v` también se borran los datos.

`LimiteInicioSesion__PeticionesPorMinuto` limita los intentos de inicio de sesión y registro, sumados, por minuto y dirección IP. `Cors__OrigenesPermitidos__0` es el origen del frontend.

## Pruebas

Las pruebas de integración levantan su propio SQL Server temporal y requieren Docker. No usan la base de datos del proyecto. En Windows, detén antes la Api para que no bloquee los archivos compilados.

```bash
cd Backend
dotnet test
```

Solo las unitarias, sin Docker:

```bash
dotnet test --filter "Categoria!=Integracion"
```

Frontend, desde la raíz:

```bash
cd Frontend
npm ci
npm test -- --watch=false
```

### Recorrido manual

1. Entra como administrador. Crea un producto con dos lotes, ábrelo, edítalo, quita un lote y guarda.
2. Intenta repetir un código o un número de lote y revisa los mensajes.
3. Elimínalo, búscalo con el estado Inactivos y reactívalo desde la edición.
4. Sal y entra como consulta. Solo verás el listado y el detalle, y `/productos/nuevo` te devolverá al listado.

### Probar el registro y los usuarios

1. Sin sesión, pulsa **Crear cuenta** en el inicio de sesión. Prueba una contraseña sin mayúscula o una confirmación distinta y revisa los mensajes.
2. Completa el registro. Volverás al inicio de sesión con un aviso. Entra con la cuenta: tiene rol Consulta, no ve **Usuarios** y `/usuarios` la devuelve al listado.
3. Repite el registro con el mismo usuario o correo: se muestra el conflicto. Con más de 5 intentos en un minuto se muestra cuánto esperar.
4. Entra como administrador y abre **Usuarios**. Crea un usuario, edítalo sin contraseña y comprueba que conserva la anterior.
5. Desactívalo y comprueba que ya no puede entrar. Reactívalo desde la edición.
6. Edita tu propia cuenta e intenta cambiar tu rol o desactivarte: la Api lo rechaza.

### Probar con Swagger

1. En `POST /api/autenticacion/login`, inicia sesión con uno de los usuarios.
2. Copia el valor de `token`, pulsa **Authorize** y pégalo sin el prefijo `Bearer`.
3. Prueba los endpoints. Con el usuario de Consulta, las escrituras responden 403.

## Solución de problemas

| Problema | Solución |
|---|---|
| Docker no responde | Abre Docker Desktop y espera a que termine de iniciar. En Linux, inicia el servicio y comprueba que tu usuario pueda usarlo |
| Puerto 1433 ocupado | Cambia `BaseDatos__Puerto` en `.env`, por ejemplo a 14333 |
| Puerto 5165 o 4200 ocupado | Ejecuta el script de detención. Si sigue ocupado, cierra el programa que lo usa, por ejemplo Visual Studio u otro `ng serve` |
| SQL Server tarda o no queda saludable | Revisa `docker compose logs sqlserver` y que Docker tenga al menos 2 GB de memoria. El script espera hasta 4 minutos |
| SQL Server no acepta la contraseña | El volumen conserva la contraseña con la que se creó. Si borraste o cambiaste `.env`, detén borrando los datos y vuelve a iniciar |
| PowerShell bloquea los scripts | Ejecútalos con `powershell -ExecutionPolicy Bypass -File`, como en el inicio rápido |
| Versión de Node.js no compatible | Instala una versión de la tabla de requisitos. Con nvm: `nvm install` en Linux y macOS, que lee `.nvmrc`; en Windows, `nvm install 24.22.1` y `nvm use 24.22.1` |
| SDK de .NET no encontrado | Instala el SDK 10.0.400 o posterior de la serie 10.0, que exige `global.json` |
| La Api o el frontend no responden | Revisa los registros de la carpeta `.ejecucion` |
| Demasiadas peticiones al iniciar sesión | Espera un minuto o cambia `LimiteInicioSesion__PeticionesPorMinuto` en `.env` y reinicia |

## Endpoints

| Método | Ruta | Acceso | Respuestas |
|---|---|---|---|
| POST | `/api/autenticacion/login` | Público, con límite de intentos | 200, 400, 401, 429 |
| POST | `/api/autenticacion/registro` | Público, con el mismo límite | 201, 400, 409, 429 |
| GET | `/api/productos` | Administrador y Consulta | 200, 400, 401 |
| GET | `/api/productos/categorias` | Administrador y Consulta | 200, 401 |
| GET | `/api/productos/{id}` | Administrador y Consulta | 200, 401, 404 |
| POST | `/api/productos` | Administrador | 201, 400, 401, 403, 409 |
| PUT | `/api/productos/{id}` | Administrador | 200, 400, 401, 403, 404, 409 |
| DELETE | `/api/productos/{id}` | Administrador | 204, 401, 403, 404 |
| GET | `/api/usuarios` | Administrador | 200, 400, 401, 403 |
| GET | `/api/usuarios/{id}` | Administrador | 200, 401, 403, 404 |
| POST | `/api/usuarios` | Administrador | 201, 400, 401, 403, 409 |
| PUT | `/api/usuarios/{id}` | Administrador | 200, 400, 401, 403, 404, 409 |
| DELETE | `/api/usuarios/{id}` | Administrador | 204, 401, 403, 404, 409 |
| GET | `/salud` | Público | 200, 503 |

El listado admite `pagina`, `tamanoPagina`, `busqueda`, `categoria`, `estado` (`Activos`, `Inactivos` o `Todos`), `precioMinimo` y `precioMaximo`.

El listado de usuarios admite `pagina` y `tamanoPagina`. `DELETE /api/usuarios/{id}` desactiva la cuenta.

Los errores se devuelven como `application/problem+json`. El valor `correlacion` coincide con el encabezado `X-Correlacion` y permite ubicar la petición en los registros, que se guardan en `Backend/src/Inventario.Api/logs`.

## Pantallas

| Ruta | Pantalla | Acceso |
|---|---|---|
| `/iniciar-sesion` | Inicio de sesión | Público |
| `/crear-cuenta` | Registro, con rol Consulta | Público |
| `/productos` | Listado con búsqueda, filtros y paginación | Administrador y Consulta |
| `/productos/{id}` | Detalle con sus lotes | Administrador y Consulta |
| `/productos/nuevo` | Crear producto | Administrador |
| `/productos/{id}/editar` | Editar producto y lotes | Administrador |
| `/usuarios` | Listado de usuarios | Administrador |
| `/usuarios/nuevo` | Crear usuario | Administrador |
| `/usuarios/{id}/editar` | Editar usuario | Administrador |

## Estructura

```text
├── Backend
│   ├── src
│   │   ├── Inventario.Dominio           Entidades y reglas de negocio
│   │   ├── Inventario.Aplicacion        Casos de uso, puertos, DTO y validadores
│   │   ├── Inventario.Infraestructura   Dapper, scripts SQL, semilla, JWT y BCrypt
│   │   └── Inventario.Api               Controllers, errores, registros y Swagger
│   └── tests
│       └── Inventario.Pruebas           Unitarias y de integración
├── Frontend
│   └── src
│       ├── app
│       │   ├── nucleo                   Sesión, interceptores, guardias, errores y diseño
│       │   ├── compartido               Componentes y utilidades reutilizables
│       │   ├── funcionalidades          Autenticación, productos y usuarios
│       │   └── pruebas                  Datos y utilidades de prueba
│       └── entornos                     URL de la Api por entorno
├── scripts                              Inicio y detención del proyecto
├── docker-compose.yml                   SQL Server
├── .env.example                         Variables requeridas
├── .nvmrc                               Versión de Node.js
└── global.json                          Versión del SDK de .NET
```

El esquema se versiona con scripts numerados en `Backend/src/Inventario.Infraestructura/Persistencia/Scripts`. Para cambiarlo, agrega un script con el siguiente número; no edites los ya aplicados.

## Despliegue

Pendiente de configurar. En producción, el frontend llama a la ruta relativa `/api`: publica ambos bajo el mismo dominio con un proxy que dirija `/api` a la Api, o cambia `urlApi` en `Frontend/src/entornos/entorno.produccion.ts` y agrega el origen del frontend a `Cors__OrigenesPermitidos__0`.

Fuera de desarrollo, la Api no aplica los scripts ni carga la semilla, y Swagger no se publica.
