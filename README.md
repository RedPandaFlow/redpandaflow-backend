# RedPandaFlow Backend

ASP.NET Core Web API for RedPandaFlow, a collaborative kanban application.

## Stack

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core 8 with PostgreSQL (Npgsql)
- JWT authentication served through HttpOnly cookies
- SignalR for real-time presence and notifications
- Clean Architecture (Domain, Application, Infrastructure, Api)
- BCrypt for password hashing
- Swagger for API exploration

## Layout

```bash
src/
├── RedPandaFlow.Domain/         # Entities, enums (no dependencies)
├── RedPandaFlow.Application/    # DTOs, service interfaces, result types
├── RedPandaFlow.Infrastructure/ # EF Core DbContext, service implementations, migrations
└── RedPandaFlow.Api/            # Controllers, SignalR hubs, host setup
```

## Prerequisites

- .NET SDK 10.0
- PostgreSQL 16 (local install or via the docker-compose stack)
- `dotnet-ef` CLI tool (for migrations):

```bash
dotnet tool install --global dotnet-ef
```

## Installation

```bash
git clone https://github.com/RedPandaFlow/redpandaflow-backend.git
cd redpandaflow-backend
dotnet restore
```

Create a `.env` file at the workspace root with at least:

```bash
JwtSettings__SecretKey=<generate with: openssl rand -base64 48>
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=redpandaflow_db;Username=redpandaflow;Password=...
```

## EF Core migrations

Run from the repository root.

Apply migrations to the database:

```bash
dotnet ef database update \
  --project src/RedPandaFlow.Infrastructure \
  --startup-project src/RedPandaFlow.Api
```

Create a new migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/RedPandaFlow.Infrastructure \
  --startup-project src/RedPandaFlow.Api
```

## Run in development

The recommended way is via the docker-compose stack in
[redpandaflow-infra](https://github.com/RedPandaFlow/redpandaflow-infra),
which also brings up PostgreSQL and pgAdmin.

For a standalone run, with PostgreSQL reachable and the `.env` file in place:

```bash
dotnet build RedPandaFlow.sln
cd src/RedPandaFlow.Api
dotnet run
```

The API listens on `http://localhost:5090` and Swagger UI is mounted at the root path `/`.

## Real-time hubs

- `/hubs/board` — board presence and per-board mutations
- `/hubs/notifications` — per-user notification stream

## Related repos

- [redpandaflow-frontend](https://github.com/RedPandaFlow/redpandaflow-frontend) — React
- [redpandaflow-infra](https://github.com/RedPandaFlow/redpandaflow-infra) — docker-compose stack
- [documentation](https://github.com/RedPandaFlow/documentation) — project documentation
