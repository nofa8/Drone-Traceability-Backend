# dTITAN Backend

Lightweight instructions to run the dTITAN backend locally (Docker or dotnet).

## Prerequisites

- Docker & Docker Compose
- \[OPTIONAL\] .NET 9 SDK (for running the backend locally)

## Table of Contents

- [Urls](#urls)
- [Run everything with Docker Compose](#run-everything-with-docker-compose)
- [Run Supporting Services In Docker Run Backend Locally](#run-supporting-services-in-docker-run-backend-locally)

## Urls

### Services

- Backend API: <http://localhost:5101>
- External websocket: <ws://localhost:8083>

### Database

- Mongo GUI (mongo-express): <http://localhost:8090> (username `admin`, password `admin`)
- Mongo DB shell / clients: connect to <mongodb://localhost:27017>

## Run everything with Docker Compose

This uses `docker-compose.yml` and will start:

- `backend`
- `external-websocket`
- `mongodb`
- `mongo-gui`

Build from repository root run:

```bash
docker compose up --build
```

## Run supporting services in Docker, run backend locally

This uses `docker-compose.dev.yml` and will start:

- `external-websocket`
- `mongodb`
- `mongo-gui`

```bash
docker compose -f docker-compose.dev.yml up --build -d
```

Then run the backend on your machine, connecting to the Docker services:

```bash
cd dTITAN.Backend
dotnet run
```

> Run `./tmux-project.sh` from the repository root, this opens panes for the backend, simulator and a docker UI.
> This script requires `tmux` and `lazydocker`, you can always edit it to not use `lazydocker`
