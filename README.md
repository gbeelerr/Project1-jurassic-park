# Project1-jurassic-park

## Jira Board
[View Jira Board](https://jurassixseven.atlassian.net/?continue=https%3A%2F%2Fjurassixseven.atlassian.net%2Fwelcome%2Fsoftware%3FprojectId%3D10000&atlOrigin=eyJpIjoiN2JkOWU5ZGQzYmM0NGI1YTlhYmRlMDI4MjI3MDFhNTEiLCJwIjoiamlyYS1zb2Z0d2FyZSJ9)

## Wireframes
### Design
[View Figma Design](https://www.figma.com/design/HqanvDwZm4pGDyuHGCjVc4/Jurassic?node-id=0-1&t=qCGNVmbppfwnLrOB-1)

### Prototype
[View Figma Prototype](https://www.figma.com/proto/HqanvDwZm4pGDyuHGCjVc4/Jurassic?node-id=0-1&t=qCGNVmbppfwnLrOB-1)

## Docker (database + APIs + web)

Prerequisites: [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine) running.

From the **repository root** — the folder that contains `docker-compose.yml` (for example `SE498` on your machine):

```bash
cd /path/to/SE498
docker compose up --build -d
```

Then open **http://localhost:5044** (Blazor web), **http://localhost:5080/movies** (movie API), and connect to Postgres on the host at **port 5433** (user `jurassic`, password `jurassic_dev`, databases `jurassic_api` and `jurassic_web`).

If `docker compose` is not found, try the older CLI: `docker-compose up --build -d`.

**Port 5432 already in use:** Compose maps Postgres to host port **5433** by default so it does not conflict with a local PostgreSQL server. To use another port: `POSTGRES_HOST_PORT=5434 docker compose up -d`. To use 5432 explicitly: `POSTGRES_HOST_PORT=5432 docker compose up -d` (stop the other service using 5432 first).