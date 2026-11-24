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

## Frontend

```bash
cd Frontend
npm ci
npm start
```
