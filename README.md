# RedPandaFlow Backend

ASP.NET Core 8 Web API for RedPandaFlow, a collaborative kanban application.

## Stack

- ASP.NET Core 8 Web API
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

## Run locally

The recommended way to run the backend is via the docker-compose stack in
[redpandaflow-infra](https://github.com/RedPandaFlow/redpandaflow-infra),
which also brings up PostgreSQL and pgAdmin.

For a standalone run, you need PostgreSQL reachable and a `.env` file at the
workspace root with at least:

```bash
JwtSettings__SecretKey=<generate with: openssl rand -base64 48>
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=redpandaflow_db;Username=redpandaflow;Password=...
```

Then:

```bash
dotnet build RedPandaFlow.sln
cd src/RedPandaFlow.Api
dotnet run
```

Swagger UI is mounted at the root path `/`.

## Real-time hubs

- `/hubs/board` — board presence and per-board mutations
- `/hubs/notifications` — per-user notification stream

## Related repos

- [redpandaflow-frontend](https://github.com/RedPandaFlow/redpandaflow-frontend) — React
- [redpandaflow-infra](https://github.com/RedPandaFlow/redpandaflow-infra) — docker-compose stack
- [documentation](https://github.com/RedPandaFlow/documentation) — project documentation
