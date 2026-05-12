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

To rerun after changes   
```bash
docker compose down --remove-orphans ; docker compose up --build
```

Then open **http://localhost:51444** (Blazor web), **http://localhost:51811/movies** (movie API), and connect to Postgres on the host at **port 55433** (user `jurassic`, password `jurassic_dev`, databases `jurassic_api` and `jurassic_web`).

If `docker compose` is not found, try the older CLI: `docker-compose up --build -d`.

**Port overrides:** Compose now uses high-number defaults to reduce conflicts:
- Web host port: `WEB_HOST_PORT` (default `51444`)
- Movie API host port: `MOVIE_API_HOST_PORT` (default `51811`)
- Movie API container port: `MOVIE_API_CONTAINER_PORT` (default `18081`)
- Postgres host port: `POSTGRES_HOST_PORT` (default `55433`)

Example override:
`POSTGRES_HOST_PORT=55434 MOVIE_API_HOST_PORT=51812 WEB_HOST_PORT=51445 docker compose up --build -d`