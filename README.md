# Inventario de productos

Prueba técnica fullstack de Wilber Alvarez.

Aplicación para administrar productos y sus lotes. Cada producto puede tener varios lotes, con fecha de ingreso, precio y cantidad propios. El usuario Administrador gestiona el inventario y el usuario Consulta solo lo ve.

- **Backend:** .NET 10, arquitectura hexagonal, Dapper, SQL Server, JWT.
- **Frontend:** Angular 22, signals, Tailwind CSS.

Las decisiones de diseño y la guía para cambios puntuales están en [DECISIONES.md](DECISIONES.md).

## Requisitos

- Git
- .NET SDK 10
- Node.js 24, con npm
- Docker en ejecución, con Docker Compose
- Puertos 4200 y 5165 libres

## Puesta en marcha

Los comandos parten de la carpeta raíz del repositorio y sirven en PowerShell y en Bash.

### 1. Crear el archivo `.env`

```bash
cp .env.example .env
```

Completa los valores vacíos de `.env`:

| Variable | Valor |
|---|---|
| `BaseDatos__Contrasena` | Contraseña de SQL Server: mínimo 8 caracteres con mayúsculas, minúsculas y números |
| `Jwt__Clave` | Clave de firma de al menos 32 caracteres |
| `Semilla__Administrador__Contrasena` | Contraseña del administrador, mínimo 8 caracteres |
| `Semilla__Consulta__Contrasena` | Contraseña del usuario de consulta, mínimo 8 caracteres |

Encierra entre comillas simples los valores con caracteres especiales. Si el puerto 1433 está ocupado, cambia `BaseDatos__Puerto`. Las variables de entorno del sistema tienen prioridad sobre `.env`.

Los demás valores funcionan como están: `Cors__OrigenesPermitidos__0` es el origen del frontend y `LimiteInicioSesion__PeticionesPorMinuto`, los intentos de inicio de sesión por minuto y dirección IP.

### 2. Iniciar la base de datos

```bash
docker compose up -d
docker compose logs sqlserver
```

Espera a que los registros muestren `Recovery is complete`. La primera vez puede tardar varios minutos por la descarga de la imagen.

### 3. Iniciar la Api

```bash
cd Backend
dotnet run --project src/Inventario.Api
```

Al arrancar en desarrollo crea la base de datos, aplica los scripts pendientes y carga los datos iniciales. Está lista cuando `http://localhost:5165/salud` responde `Healthy`.

### 4. Iniciar el frontend

En otra terminal, desde la raíz:

```bash
cd Frontend
npm ci
npm start
```

### 5. Iniciar sesión

Abre `http://localhost:4200` con uno de los usuarios de la semilla:

| Rol | Usuario | Contraseña |
|---|---|---|
| Administrador | Valor de `Semilla__Administrador__NombreUsuario` (`admin` en el ejemplo) | Valor de `Semilla__Administrador__Contrasena` |
| Consulta | Valor de `Semilla__Consulta__NombreUsuario` (`consulta` en el ejemplo) | Valor de `Semilla__Consulta__Contrasena` |

Los usuarios se crean solo si no existen: cambiar después la contraseña en `.env` no modifica la ya guardada.

Para detener la base de datos usa `docker compose down`. Con `docker compose down -v` también se borran sus datos.

## Direcciones

| Servicio | URL |
|---|---|
| Frontend | `http://localhost:4200` |
| Api | `http://localhost:5165/api` |
| Swagger | `http://localhost:5165/swagger` |
| Estado | `http://localhost:5165/salud` |

## Pruebas

Las pruebas de integración levantan su propio SQL Server temporal y requieren Docker en ejecución. No usan la base de datos del paso 2.

En Windows, detén antes la Api para que no bloquee los archivos compilados.

```bash
cd Backend
dotnet build
dotnet test
```

Solo las unitarias, sin Docker:

```bash
dotnet test --filter "Categoria!=Integracion"
```

Frontend, desde la raíz:

```bash
cd Frontend
npm run build
npm test -- --watch=false
```

### Recorrido manual

1. Entra como administrador. Crea un producto con dos lotes, ábrelo, edítalo, quita un lote y guarda.
2. Intenta repetir un código o un número de lote y revisa los mensajes.
3. Elimínalo, búscalo con el estado Inactivos y reactívalo desde la edición.
4. Sal y entra como consulta. Solo verás el listado y el detalle, y `/productos/nuevo` te devolverá al listado.

### Probar con Swagger

1. En `POST /api/autenticacion/login`, inicia sesión con uno de los usuarios.
2. Copia el valor de `token`, pulsa **Authorize** y pégalo sin el prefijo `Bearer`.
3. Prueba los endpoints. Con el usuario de Consulta, las escrituras responden 403.

## Endpoints

| Método | Ruta | Acceso | Respuestas |
|---|---|---|---|
| POST | `/api/autenticacion/login` | Público, con límite de intentos | 200, 400, 401, 429 |
| GET | `/api/productos` | Administrador y Consulta | 200, 400, 401 |
| GET | `/api/productos/categorias` | Administrador y Consulta | 200, 401 |
| GET | `/api/productos/{id}` | Administrador y Consulta | 200, 401, 404 |
| POST | `/api/productos` | Administrador | 201, 400, 401, 403, 409 |
| PUT | `/api/productos/{id}` | Administrador | 200, 400, 401, 403, 404, 409 |
| DELETE | `/api/productos/{id}` | Administrador | 204, 401, 403, 404 |
| GET | `/salud` | Público | 200, 503 |

El listado admite `pagina`, `tamanoPagina`, `busqueda`, `categoria`, `estado` (`Activos`, `Inactivos` o `Todos`), `precioMinimo` y `precioMaximo`.

Los errores se devuelven como `application/problem+json`. El valor `correlacion` coincide con el encabezado `X-Correlacion` y permite ubicar la petición en los registros, que se guardan en `Backend/src/Inventario.Api/logs`.

## Pantallas

| Ruta | Pantalla | Acceso |
|---|---|---|
| `/iniciar-sesion` | Inicio de sesión | Público |
| `/productos` | Listado con búsqueda, filtros y paginación | Administrador y Consulta |
| `/productos/{id}` | Detalle con sus lotes | Administrador y Consulta |
| `/productos/nuevo` | Crear producto | Administrador |
| `/productos/{id}/editar` | Editar producto y lotes | Administrador |

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
│       │   ├── funcionalidades          Autenticación y productos
│       │   └── pruebas                  Datos y utilidades de prueba
│       └── entornos                     URL de la Api por entorno
├── docker-compose.yml                   SQL Server
├── .env.example                         Variables requeridas
└── DECISIONES.md
```

El esquema se versiona con scripts numerados en `Backend/src/Inventario.Infraestructura/Persistencia/Scripts`. Para cambiarlo, agrega un script con el siguiente número; no edites los ya aplicados.

## Despliegue

Pendiente de configurar. En producción, el frontend llama a la ruta relativa `/api`: publica ambos bajo el mismo dominio con un proxy que dirija `/api` a la Api, o cambia `urlApi` en `Frontend/src/entornos/entorno.produccion.ts` y agrega el origen del frontend a `Cors__OrigenesPermitidos__0`.

Fuera de desarrollo, la Api no aplica los scripts ni carga la semilla, y Swagger no se publica.
