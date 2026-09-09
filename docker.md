# Docker & Docker Compose — A Step-by-Step Guide

This guide walks through Docker from zero, using the files already in this repo
(`productsManangement/Dockerfile`, `docker-compose.yml`, `docker-compose.override.yml`)
as the running example. Read top to bottom the first time; use it as reference after.

## Table of contents

1. [What is Docker?](#1-what-is-docker)
2. [Images vs. Containers](#2-images-vs-containers)
3. [What is a Dockerfile?](#3-what-is-a-dockerfile)
4. [Walking through this project's Dockerfile](#4-walking-through-this-projects-dockerfile)
5. [Building and running a single container](#5-building-and-running-a-single-container)
6. [What is Docker Compose?](#6-what-is-docker-compose)
7. [Anatomy of a compose file](#7-anatomy-of-a-compose-file)
8. [This project's docker-compose.yml, explained](#8-this-projects-docker-composeyml-explained)
9. [What is docker-compose.override.yml, and why keep it separate?](#9-what-is-docker-composeoverrideyml-and-why-keep-it-separate)
10. [Writing a compose service for SQL Server (MSSQL)](#10-writing-a-compose-service-for-sql-server-mssql)
11. [Writing a compose service for PostgreSQL](#11-writing-a-compose-service-for-postgresql)
12. [Connection strings: what they are and why they look like that](#12-connection-strings-what-they-are-and-why-they-look-like-that)
13. [TrustServerCertificate, Encrypt, and TLS in dev vs. prod](#13-trustservercertificate-encrypt-and-tls-in-dev-vs-prod)
14. [Volumes: why your data survives (or doesn't)](#14-volumes-why-your-data-survives-or-doesnt)
15. [Command cheat sheet](#15-command-cheat-sheet)
16. [Troubleshooting: real mistakes made in this repo](#16-troubleshooting-real-mistakes-made-in-this-repo)
17. [Exercises](#17-exercises)

---

## 1. What is Docker?

Docker packages an application **together with everything it needs to run**
(runtime, libraries, config, OS files) into a single portable unit called an
**image**. You run that image as a **container** — an isolated process on
your machine that behaves the same on your laptop, your teammate's laptop,
and a production server, because it's not relying on whatever happens to be
installed on the host.

This solves the classic "works on my machine" problem: instead of installing
.NET 10, SQL Server, and configuring them just right on every developer's PC,
everyone runs the same container image.

```
 Without Docker                         With Docker
 ---------------                        -----------
 Your laptop                            Your laptop
   .NET 9? .NET 10?  --- different -->    [ Container: .NET 10 + app ]
   SQL Server 2019? 2022?                 [ Container: SQL Server 2017 ]
   "works here, breaks there"             Same images run identically
                                          on any machine with Docker.
```

A **container is not a virtual machine**. A VM virtualizes an entire
computer, including its own kernel — heavy, slow to boot. A container shares
the host machine's kernel and just isolates processes/filesystem/network, so
it starts in milliseconds and uses a fraction of the resources.

```
 Virtual Machines                    Containers
 ┌─────────┐ ┌─────────┐             ┌─────────┐ ┌─────────┐
 │  App A  │ │  App B  │             │  App A  │ │  App B  │
 │ Guest OS│ │ Guest OS│             ├─────────┴─┴─────────┤
 ├─────────┴─┴─────────┤             │   Docker Engine      │
 │     Hypervisor       │             ├──────────────────────┤
 ├──────────────────────┤             │      Host OS         │
 │       Host OS        │             ├──────────────────────┤
 └──────────────────────┘             │      Hardware        │
                                      └──────────────────────┘
```

---

## 2. Images vs. Containers

- An **image** is a read-only template: filesystem layers + metadata (what
  command to run, what ports to expose, etc.). Think of it as a class.
- A **container** is a running (or stopped) instance of an image. Think of it
  as an object created from that class. You can start many containers from
  one image.
- A **registry** (e.g. Docker Hub, `mcr.microsoft.com`) is where images are
  stored and pulled from — like npm/NuGet, but for images.

```
   docker build            docker run
 Dockerfile ───────► Image ───────► Container (running process)
                        │
                        │ docker push / pull
                        ▼
                    Registry (Docker Hub, mcr.microsoft.com, ...)
```

---

## 3. What is a Dockerfile?

A `Dockerfile` is a recipe: a text file of instructions describing how to
build an image — starting from some base image, then copying in your code,
installing dependencies, and declaring how to start the app.

Common instructions:

| Instruction  | Meaning                                              |
|--------------|-------------------------------------------------------|
| `FROM`       | base image to start from                              |
| `WORKDIR`    | sets the "current directory" inside the image          |
| `COPY`       | copies files from your machine into the image         |
| `RUN`        | executes a command *while building* the image          |
| `EXPOSE`     | documents which port(s) the container listens on       |
| `ENTRYPOINT` | the command that runs when a container *starts*         |
| `ARG`        | a build-time variable                                   |
| `USER`       | which OS user the container runs as (security)          |

A **multi-stage build** uses more than one `FROM` in the same file, so a
heavy SDK (needed only to *compile* the app) doesn't end up in the final,
smaller runtime image. That's exactly what this project does.

---

## 4. Walking through this project's Dockerfile

File: `productsManangement/Dockerfile`

```dockerfile
# Stage 1: "base" — the lean runtime image the final container will use
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID          # run as a non-root user (security best practice)
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: "build" — has the full SDK, used only to compile the code
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["productsManangement/productsManangement.csproj", "productsManangement/"]
RUN dotnet restore "./productsManangement/productsManangement.csproj"
COPY . .
WORKDIR "/src/productsManangement"
RUN dotnet build "./productsManangement.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: "publish" — produces the final, deployable output
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./productsManangement.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: "final" — copies only the published output onto the lean base image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "productsManangement.dll"]
```

```
   build stage (SDK image, ~1GB+)           final image (aspnet runtime, small)
 ┌───────────────────────────────┐        ┌───────────────────────────┐
 │ restore → build → publish     │  COPY  │ /app/publish contents only│
 │ (source code, SDK, caches)    │ ─────► │ + aspnet runtime           │
 └───────────────────────────────┘        └───────────────────────────┘
        thrown away after build                  this is what ships
```

Why `COPY ["productsManangement/productsManangement.csproj", ...]` *before*
`COPY . .`? So `dotnet restore` (slow — downloads NuGet packages) is cached
by Docker as its own layer and only reruns when the `.csproj` changes, not
every time *any* source file changes.

Notice `WORKDIR /src` then later `WORKDIR "/src/productsManangement"` — the
build stage's *root* is the whole repo (`context: .` in compose, see below),
but the actual `.csproj`/code live inside the `productsManangement/`
subfolder, so the working directory has to move into it before building.

---

## 5. Building and running a single container

You *can* work with a Dockerfile directly, without Compose:

```bash
# Build an image from the Dockerfile, tag it "products-api"
docker build -f productsManangement/Dockerfile -t products-api .

# Run a container from that image, mapping host port 8090 to container port 8080
docker run -p 8090:8080 --name products-api products-api
```

This works fine for a single service. It gets painful once you need *two*
containers that must talk to each other (the API + a database) — you'd have
to manually create a network, start both containers in the right order, wire
up env vars, etc. That's the problem Compose solves.

---

## 6. What is Docker Compose?

Docker Compose lets you describe **multiple containers, their networking,
and their configuration** in one YAML file, then bring the whole stack up or
down with one command. Every service in the file automatically gets:

- its own container,
- a shared private network with the other services, so they can reach each
  other **by service name** (e.g. `ms-sql-server`) like a hostname,
- environment variables, port mappings, volumes, and startup order rules
  (`depends_on`) that you declare once and get every time.

```
                docker compose up
                        │
                        ▼
        ┌───────────────────────────────────┐
        │   compose network (private)        │
        │                                    │
        │  ┌───────────────┐  ┌───────────┐  │
        │  │ productsmanang-│  │ ms-sql-   │  │
        │  │ ement (API)   │─▶│ server    │  │
        │  │ :8080         │  │ :1433     │  │
        │  └───────┬───────┘  └─────┬─────┘  │
        └──────────┼────────────────┼────────┘
                    │                │
              host:8090        host:1433
```

The API container reaches the database simply via
`Server=ms-sql-server,1433` — Compose's built-in DNS resolves the service
name to the right container IP. No hardcoded IPs, ever.

---

## 7. Anatomy of a compose file

```yaml
services:                 # required — one entry per container
  <service-name>:
    image: ...             #   OR build a custom image:
    build:
      context: .            # folder sent to the Docker build
      dockerfile: path/to/Dockerfile
    environment:            # env vars injected into the container
      - KEY=value
    ports:
      - "hostPort:containerPort"
    volumes:
      - name-or-path:/container/path
    depends_on:
      other-service:
        condition: service_healthy   # wait for its healthcheck to pass
    healthcheck:            # how Compose checks if the container is "ready"
      test: ["CMD", "..."]
      interval: 10s
      timeout: 5s
      retries: 10

volumes:                  # named volumes referenced above, declared once
  name-or-path:

networks:                 # optional — Compose creates a default one for you
  ...
```

The top-level `services:` key is **mandatory** — without it, Compose has no
idea your service blocks are services (see [section 16](#16-troubleshooting-real-mistakes-made-in-this-repo)
for what happens when it's missing).

---

## 8. This project's docker-compose.yml, explained

```yaml
services:
  ms-sql-server:
    image: mcr.microsoft.com/mssql/server:2017-latest-ubuntu
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "Pa5S5w0rd2021"
      MSSQL_PID: Express
    ports:
      - "1433:1433"
    healthcheck:
      test: ["CMD", "/opt/mssql-tools/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", "Pa5S5w0rd2021", "-Q", "SELECT 1"]
      interval: 10s
      timeout: 5s
      retries: 10
    volumes:
      - mssql-data:/var/opt/mssql

  productsmanangement:
    build:
      context: .
      dockerfile: productsManangement/Dockerfile
    ports:
      - "8090:8080"
    depends_on:
      ms-sql-server:
        condition: service_healthy

volumes:
  mssql-data:
```

Line by line:

- **`ms-sql-server`** — uses a *pre-built* image (`image:`), not a custom
  build, because we don't need to change SQL Server itself, just configure it
  via environment variables (`ACCEPT_EULA`, `SA_PASSWORD`, `MSSQL_PID`).
- **`healthcheck`** — Compose runs `sqlcmd ... SELECT 1` every 10 seconds; if
  it succeeds, the container is marked "healthy". This matters because SQL
  Server takes several seconds to actually accept connections after the
  process starts — the container "running" and the database "ready" are two
  different moments.
- **`volumes: mssql-data:/var/opt/mssql`** — `/var/opt/mssql` is where SQL
  Server stores its data files. Mounting a *named volume* there means the
  data survives `docker compose down` and container rebuilds (see
  [section 14](#14-volumes-why-your-data-survives-or-doesnt)).
- **`productsmanangement`** — uses `build:` instead of `image:`, because this
  *is* our code. `context: .` means "send the whole repo root to the Docker
  build" (needed because the Dockerfile's `COPY . .` step expects the full
  repo, not just the `productsManangement/` subfolder). `dockerfile:` points
  at the actual file since it isn't at the repo root.
- **`depends_on: ms-sql-server: condition: service_healthy`** — don't just
  start the API when the *container* for SQL Server exists; wait until its
  healthcheck passes. Without `condition: service_healthy`, `depends_on`
  only guarantees start *order*, not readiness — the API could start before
  SQL Server can accept logins and crash.
- **`volumes:` (top-level)** — every named volume used above must be declared
  once here, even with no extra config (`mssql-data:` with nothing after the
  colon just means "use the defaults").

---

## 9. What is docker-compose.override.yml, and why keep it separate?

Compose automatically loads `docker-compose.override.yml` **on top of**
`docker-compose.yml` if both are present, merging them field by field. It
exists so the base file can describe the stack in an environment-agnostic
way, while the override injects **local-development-only** concerns.

```yaml
services:
  productsmanangement:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
    ports:
      - "8090:8080"
    volumes:
      - ${APPDATA}/Microsoft/UserSecrets:/home/app/.microsoft/usersecrets:ro
      - ${APPDATA}/Microsoft/UserSecrets:/root/.microsoft/usersecrets:ro
```

Why this is worth keeping separate rather than folding into one file:

- `ASPNETCORE_ENVIRONMENT=Development` should never be baked into the base
  file — a production or CI compose run shouldn't silently be "Development".
- The volume mounts pull **your Windows user's** .NET User Secrets folder
  (`%APPDATA%\Microsoft\UserSecrets`) into the container, read-only, so
  secrets you set on your host with `dotnet user-secrets set` are visible
  inside the container too. This is a *machine-specific, developer-specific*
  path — it has no business in a file meant to describe the app for any
  environment.

The **merge rule**: a service in the override is only merged into the base
service with the **exact same name**. `productsmanangement` here must match
`productsmanangement` in `docker-compose.yml` character-for-character — a
typo or a "fixed" spelling in one file and not the other silently creates
*two separate services* instead of merging one. (This bit us during this
project — see [section 16](#16-troubleshooting-real-mistakes-made-in-this-repo).)

```
 docker-compose.yml            docker-compose.override.yml
 ┌─────────────────┐           ┌───────────────────────┐
 │ productsmanang-  │  merge   │ productsmanang-        │
 │ ement:           │◄────────►│ ement:                │
 │  build: ...      │  (same   │  environment: [...]    │
 │  ports: 8090:8080│   name!) │  volumes: [...]         │
 └─────────────────┘           └───────────────────────┘
           │                              │
           └──────────────┬───────────────┘
                           ▼
                one container, merged config
```

---

## 10. Writing a compose service for SQL Server (MSSQL)

Minimal, standalone example:

```yaml
services:
  ms-sql-server:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "YourStrong!Passw0rd"   # note: 2019+ images use MSSQL_SA_PASSWORD
      MSSQL_PID: Developer
    ports:
      - "1433:1433"
    volumes:
      - mssql-data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -No -Q 'SELECT 1' || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10

volumes:
  mssql-data:
```

Notes:
- On the `2017-*` image family this repo uses, the password variable is
  `SA_PASSWORD`; on `2019` and later it's `MSSQL_SA_PASSWORD` — always check
  the image tag's documentation, this is a common source of "container
  starts then immediately exits" bugs.
- SQL Server enforces a **strong password policy** (length, mixed case,
  digits, symbols) — a weak `SA_PASSWORD` makes the container exit
  immediately on first run.
- `MSSQL_PID` selects the edition (`Express`, `Developer`, `Standard`, ...).
  `Developer` is free and has no resource caps — good for local dev.

## 11. Writing a compose service for PostgreSQL

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: appuser
      POSTGRES_PASSWORD: "YourStrong!Passw0rd"
      POSTGRES_DB: productsManagementDb
    ports:
      - "5432:5432"
    volumes:
      - pg-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U appuser -d productsManagementDb"]
      interval: 10s
      timeout: 5s
      retries: 10

volumes:
  pg-data:
```

Differences from the SQL Server setup worth noticing:

- Postgres's official image creates `POSTGRES_DB` for you automatically on
  first run — SQL Server's image does **not** create your application
  database; that's why this project needed an EF Core migration
  (`Database.Migrate()`) to create `productsManagementDb` itself.
- Postgres's data directory is `/var/lib/postgresql/data`, not
  `/var/opt/mssql`.
- `pg_isready` is Postgres's own built-in readiness-check binary — simpler
  than SQL Server's `sqlcmd`-based healthcheck.

---

## 12. Connection strings: what they are and why they look like that

A connection string tells the app's database driver **where** the database
is and **how** to authenticate. This project's (`appsettings.Development.json`):

```
Server=ms-sql-server,1433;Database=productsManagementDb;User Id=sa;Password=Pa5S5w0rd2021;TrustServerCertificate=True;Encrypt=False
```

| Part                          | Meaning                                                         |
|--------------------------------|------------------------------------------------------------------|
| `Server=ms-sql-server,1433`    | hostname (the **Compose service name**, not `localhost`!) + port |
| `Database=productsManagementDb`| which database on that server to connect to                     |
| `User Id=sa` / `Password=...`  | credentials to authenticate with                                 |
| `TrustServerCertificate=True`  | see section 13                                                   |
| `Encrypt=False`                | see section 13                                                   |

**Why `ms-sql-server`, not `localhost`, as the host?** From *inside* the
`productsmanangement` container, `localhost` means "this container itself" —
SQL Server is a *different* container. Compose's internal DNS resolves the
service name `ms-sql-server` to the right container automatically, because
both containers sit on the same Compose network. If you instead ran the app
directly on your Windows machine (outside Docker) against the same SQL
Server container, you'd use `Server=localhost,1433` — because *from the
host's point of view*, the container's exposed port *is* on localhost.

```
 From INSIDE a container            From the HOST machine (outside Docker)
 (this app's own container)
   Server=ms-sql-server,1433          Server=localhost,1433
        │                                   │
        ▼                                   ▼
   resolved via Compose DNS           resolved via the "1433:1433"
   to the ms-sql-server container      port mapping in docker-compose.yml
```

A Postgres connection string (e.g. via Npgsql in .NET) follows the same idea,
different syntax:

```
Host=postgres;Port=5432;Database=productsManagementDb;Username=appuser;Password=YourStrong!Passw0rd
```

---

## 13. TrustServerCertificate, Encrypt, and TLS in dev vs. prod

Modern SQL Server drivers (Microsoft.Data.SqlClient) **encrypt the
connection by default** and validate the server's TLS certificate against a
trusted Certificate Authority — same idea as your browser checking a
website's HTTPS certificate.

The SQL Server container in this repo doesn't have a certificate signed by a
real CA — it self-signs one on startup. Two options exist to deal with that:

- **`TrustServerCertificate=True`** — "encrypt the connection, but skip
  validating the certificate's authenticity." You still get encryption on
  the wire, just no proof the server is who it claims to be. Fine for local
  development where you already trust the machine/container you're talking
  to.
- **`Encrypt=False`** — disable encryption entirely. Only ever reasonable on
  a fully trusted local network for development; **never** in production.

```
 Production (real CA-signed cert)      Local dev container (self-signed cert)
 ┌────────┐  verified cert  ┌──────┐   ┌────────┐  unverifiable cert ┌──────┐
 │  App   │ ───────────────►│  DB  │   │  App   │ ───X (fails)──────►│  DB  │
 └────────┘                 └──────┘   └────────┘                    └──────┘
   Encrypt=True                          TrustServerCertificate=True
   TrustServerCertificate=False          "encrypt, but don't verify the cert"
   (default, and correct)                (dev-only escape hatch)
```

**Rule of thumb:** `TrustServerCertificate=True` / `Encrypt=False` are
acceptable in `appsettings.Development.json` talking to a local container.
In `appsettings.Production.json` (or whatever config feeds a real
deployment), the database should have a properly signed certificate, and
both settings should be left at their secure defaults
(`Encrypt=True`/mandatory, `TrustServerCertificate=False`).

---

## 14. Volumes: why your data survives (or doesn't)

By default, anything a container writes to its own filesystem is **lost**
when the container is removed (`docker compose down`, or Compose recreating
it after a config change). A **volume** is storage that lives *outside* the
container's lifecycle — Docker manages it separately, so it survives
container removal.

```
 no volume:                              with a volume:
 ┌───────────┐                           ┌───────────┐        ┌────────────┐
 │ container │──writes data──►(gone      │ container │───────►│  volume     │
 │           │   when removed)  when     │           │        │ (persists   │
 └───────────┘                 removed   └───────────┘        │  on disk)   │
                                                                └────────────┘
```

In this repo:

```yaml
    volumes:
      - mssql-data:/var/opt/mssql   # named volume, managed by Docker
volumes:
  mssql-data:                       # declared once, so Compose knows to create/reuse it
```

- `docker compose down` — stops and removes containers, **keeps** volumes.
- `docker compose down -v` — also deletes the named volumes (data gone for
  good).
- The `docker-compose.override.yml` volumes are a *different kind* — **bind
  mounts** (`${APPDATA}/...:/container/path`), which map an exact folder on
  your host machine into the container, rather than letting Docker manage
  the storage. Good for things like user secrets/certs you want to author on
  the host; not appropriate for a database's actual data files.

---

## 15. Command cheat sheet

```bash
docker compose up            # start the whole stack (build if images are missing)
docker compose up --build    # force a rebuild of any service using "build:"
docker compose up -d         # same, detached (runs in the background)
docker compose ps            # list this stack's containers and their status
docker compose logs -f api   # follow logs for one service (replace "api" with service name)
docker compose down          # stop and remove containers + networks (keeps volumes)
docker compose down -v       # same, and also delete named volumes (data loss!)
docker compose config        # print the fully merged, resolved config — great for debugging
docker compose exec ms-sql-server bash   # open a shell inside a running container
```

---

## 16. Troubleshooting: real mistakes made in this repo

These actually happened while building this stack — good to recognize:

1. **`docker compose config` says `additional properties '...' not allowed`.**
   The top-level `services:` key was accidentally deleted, so Compose sees
   your service blocks as unknown root-level properties instead of services.
   Fix: everything under `services:`, indented one level in.

2. **Override doesn't seem to apply / two containers show up instead of one
   merged one.** The service name in `docker-compose.override.yml` doesn't
   exactly match the one in `docker-compose.yml` (e.g. one has a typo the
   other doesn't). Fix: make the names byte-for-byte identical.

3. **`dockerfile: someFolder/Dockerfile` — build fails, "path not found".**
   The path is case- and spelling-sensitive and must match the real folder
   on disk exactly, especially since Docker builds run in a Linux
   environment even when you're on Windows.

4. **App crashes: "Unable to configure HTTPS endpoint... No server
   certificate was specified."** You enabled `ASPNETCORE_HTTPS_PORTS` without
   giving Kestrel an actual certificate file + password
   (`ASPNETCORE_Kestrel__Certificates__Default__Path` /
   `...__Password`) and a volume mounting that `.pfx` into the container.
   Either wire that up, or don't expose an HTTPS port from the container at
   all — HTTP on a mapped host port is often enough for local dev.

5. **App logs: "Login failed for user 'sa'" / "Cannot open database
   ...".** The container for SQL Server is running fine, but the actual
   *application database* was never created — the SQL Server image doesn't
   create your app's database for you (unlike Postgres's image, which does
   via `POSTGRES_DB`). Fix: create an EF Core migration and call
   `dbContext.Database.Migrate()` at startup so the schema is created
   automatically the first time the app runs against a fresh database.

6. **`depends_on` without `condition: service_healthy`.** The dependent
   service starts as soon as the database *container* exists, not once the
   database is actually ready to accept connections — a race condition that
   "usually works" locally and then randomly fails. Always pair
   `depends_on` with a `healthcheck` + `condition: service_healthy` for
   databases.

---

## 17. Exercises

Try these, in order, to build confidence:

1. Run `docker compose config` in this repo and read the fully merged
   output — match every line back to `docker-compose.yml` and
   `docker-compose.override.yml`.
2. Break something on purpose: rename the `productsmanangement` service in
   just the override file, run `docker compose config`, and observe what
   happens to the merge.
3. Add a Postgres service to a scratch compose file using section 11, bring
   it up, and connect to it with a GUI tool (e.g. Azure Data Studio, DBeaver)
   using `localhost:5432`.
4. Change `Encrypt=False` to `Encrypt=True` (keeping
   `TrustServerCertificate=True`) in the connection string and confirm the
   app still connects — notice nothing else had to change, because
   `TrustServerCertificate` only controls certificate *validation*, not
   whether encryption happens at all.
5. Run `docker compose down -v`, then `docker compose up` again, and confirm
   the `Products` table gets recreated automatically (via the EF Core
   migration) with no data in it — proving persistence really did depend on
   the named volume, not on the migration.
