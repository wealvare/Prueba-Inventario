# Inventario de productos

Prueba técnica fullstack de Wilber Alvarez.

## Requisitos

- .NET SDK 10
- Node.js 24
- Docker

## Base de datos

Copia `.env.example` como `.env`, define una contraseña segura para SQL Server y ejecuta:

```bash
docker compose up -d
```

## Backend

```bash
cd Backend
dotnet build
dotnet test
dotnet run --project src/Inventario.Api
```

## Frontend

```bash
cd Frontend
npm ci
npm start
```
