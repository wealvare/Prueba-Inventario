# Inventario de productos

Prueba técnica fullstack de Wilber Alvarez.

El acceso a datos usa Dapper para escribir y controlar el SQL de cada consulta. El detalle está en `DECISIONES.md`.

## Requisitos

- .NET SDK 10
- Node.js 24
- Docker

## Configuración

Copia `.env.example` como `.env` y completa los valores vacíos:

- `BaseDatos__Contrasena`: contraseña de SQL Server. Debe cumplir su política de complejidad.
- `Jwt__Clave`: clave de firma de al menos 32 bytes.
- `Semilla__Administrador__Contrasena` y `Semilla__Consulta__Contrasena`: contraseñas de los usuarios iniciales, de al menos 8 caracteres.
- `Cors__OrigenesPermitidos__0`: origen del frontend. Para varios, usa los índices siguientes.
- `LimiteInicioSesion__PeticionesPorMinuto`: intentos de inicio de sesión por minuto y dirección IP.

Encierra entre comillas simples los valores con caracteres especiales. Si el puerto 1433 está ocupado, cambia `BaseDatos__Puerto`. Las variables de entorno tienen prioridad sobre `.env`.

## Base de datos

```bash
docker compose up -d
```

En desarrollo, la Api crea la base de datos, aplica los scripts pendientes y carga los usuarios y productos iniciales al arrancar.

El esquema se versiona con DbUp en `Backend/src/Inventario.Infraestructura/Persistencia/Scripts`. Para cambiarlo, agrega un script con el siguiente número; no edites los ya aplicados.

## Backend

```bash
cd Backend
dotnet build
dotnet test
dotnet run --project src/Inventario.Api
```

Las pruebas de integración levantan un SQL Server temporal y requieren Docker. Para ejecutar solo las unitarias:

```bash
dotnet test --filter "Categoria!=Integracion"
```

## Endpoints

| Método | Ruta | Acceso | Respuestas |
|---|---|---|---|
| POST | `/api/autenticacion/login` | Público, con límite de intentos | 200, 400, 401, 429 |
| GET | `/api/productos` | Administrador y Consulta | 200, 400, 401 |
| GET | `/api/productos/{id}` | Administrador y Consulta | 200, 401, 404 |
| POST | `/api/productos` | Administrador | 201, 400, 401, 403, 409 |
| PUT | `/api/productos/{id}` | Administrador | 200, 400, 401, 403, 404, 409 |
| DELETE | `/api/productos/{id}` | Administrador | 204, 401, 403, 404 |
| GET | `/salud` | Público | 200, 503 |

El listado admite `pagina`, `tamanoPagina`, `busqueda`, `categoria`, `estado` (`Activos`, `Inactivos` o `Todos`), `precioMinimo` y `precioMaximo`.

Los errores se devuelven como `application/problem+json`. El valor `correlacion` coincide con el encabezado `X-Correlacion` y permite ubicar la petición en `logs/`.

## Probar con Swagger

1. Ejecuta la Api y abre `http://localhost:5165/swagger`.
2. En `POST /api/autenticacion/login`, inicia sesión con uno de los usuarios definidos en `.env`.
3. Copia el valor de `token`, pulsa **Authorize** y pégalo sin el prefijo `Bearer`.
4. Prueba los endpoints. Con el usuario de Consulta, las escrituras responden 403.

## Frontend

```bash
cd Frontend
npm ci
npm start
```
