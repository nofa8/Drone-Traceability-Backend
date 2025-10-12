# dTITAN Backend

This repository contains the dTITAN backend (ASP.NET Core 9) which uses MongoDB and Redis for storage and caching. This README documents how to run the project locally using Docker Compose or via the `dotnet` CLI.

## Prerequisites

- Docker & Docker Compose (to run MongoDB, Redis and the backend in containers)
- .NET 9.0 SDK (to run backend locally)

## Quick start

Start services with Docker Compose (from the repository root):

```bash
docker compose up --build
```

This will start the following services:

- `mongodb` (27017)
- `mongo-gui` (8081)
- `redis` (6379)
- `redis-gui` (8082)
- `backend` (5000)
