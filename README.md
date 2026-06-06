# RedPandaFlow Backend

API web ASP.NET Core de RedPandaFlow, une application de kanban collaboratif.

## Présentation

RedPandaFlow permet aux équipes d'organiser leur travail en espaces de travail,
tableaux, colonnes et cartes, avec une synchronisation en temps réel entre
collaborateurs. Ce dépôt héberge l'API backend : authentification, gestion des
espaces/tableaux/colonnes/cartes, hubs temps réel et notifications.

L'architecture globale (services et communication) est documentée dans le
[dépôt documentation](https://github.com/RedPandaFlow/documentation/blob/main/architecture.md).

## Équipe

Travail collaboratif sur l'ensemble du projet (backend, frontend, infra,
CI/CD, documentation) :

- Nathan FERRE
- Ylan Dessenne

## Stack

- API web ASP.NET Core (.NET 10)
- Entity Framework Core avec PostgreSQL (Npgsql)
- Authentification JWT servie via cookies HttpOnly
- SignalR pour la présence et les notifications en temps réel
- Clean Architecture (Domain, Application, Infrastructure, Api)
- BCrypt pour le hachage des mots de passe
- Swagger pour explorer l'API

## Organisation du code

```bash
src/
├── RedPandaFlow.Domain/         # Entités, enums (aucune dépendance)
├── RedPandaFlow.Application/    # DTOs, interfaces de services, types de résultat
├── RedPandaFlow.Infrastructure/ # DbContext EF Core, implémentations de services, migrations
└── RedPandaFlow.Api/            # Contrôleurs, hubs SignalR, configuration de l'hôte
```

## Prérequis

- SDK .NET 10.0
- PostgreSQL 16 (installation locale ou via la stack docker-compose)
- L'outil CLI `dotnet-ef` (pour les migrations) :

```bash
dotnet tool install --global dotnet-ef
```

## Installation

```bash
git clone https://github.com/RedPandaFlow/redpandaflow-backend.git
cd redpandaflow-backend
dotnet restore
```

Créer un fichier `.env` à la racine du workspace avec au minimum :

```bash
JwtSettings__SecretKey=<générer avec : openssl rand -base64 48>
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=redpandaflow_db;Username=redpandaflow;Password=...
```

## Migrations EF Core

À exécuter depuis la racine du dépôt.

Appliquer les migrations à la base :

```bash
dotnet ef database update \
  --project src/RedPandaFlow.Infrastructure \
  --startup-project src/RedPandaFlow.Api
```

Créer une nouvelle migration :

```bash
dotnet ef migrations add <NomDeLaMigration> \
  --project src/RedPandaFlow.Infrastructure \
  --startup-project src/RedPandaFlow.Api
```

## Lancement en développement

La méthode recommandée est la stack docker-compose du dépôt
[redpandaflow-infra](https://github.com/RedPandaFlow/redpandaflow-infra),
qui démarre aussi PostgreSQL et pgAdmin.

Pour un lancement autonome, avec PostgreSQL accessible et le fichier `.env` en place :

```bash
dotnet build RedPandaFlow.sln
cd src/RedPandaFlow.Api
dotnet run
```

L'API écoute sur `http://localhost:5090` et l'interface Swagger est montée à la racine `/`.

## Hubs temps réel

- `/hubs/board` — présence et mutations par tableau
- `/hubs/notifications` — flux de notifications par utilisateur

## Dépôts liés

- [redpandaflow-frontend](https://github.com/RedPandaFlow/redpandaflow-frontend) — React
- [redpandaflow-infra](https://github.com/RedPandaFlow/redpandaflow-infra) — stack docker-compose
- [documentation](https://github.com/RedPandaFlow/documentation) — documentation du projet
