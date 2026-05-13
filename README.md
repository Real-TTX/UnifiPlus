# UnifiPlus

`UnifiPlus` is an ASP.NET Core web app for managing UniFi-based WAN switching policies per user and client device.

## MVP idea

- Users sign in to the web app.
- A user sees UniFi clients and assigns devices to themselves.
- The app stores assignments in UniFi by using a naming convention like `UP-{UserId}-{Index}`.
- A WAN slider updates a UniFi policy in the background.

## Project structure

- `src/UnifiPlus.Web`: ASP.NET Core MVC app

## Run with Docker

```bash
docker compose up --build
```

This starts two containers:

- `unifiplus-dev`: local development build on [http://localhost:8080](http://localhost:8080)
- `unifiplus-master`: published GitHub image on [http://localhost:8081](http://localhost:8081)

The local container names are `unifiplus-dev` and `unifiplus-master`.

## Publish To GitHub

The repository now includes a GitHub Actions workflow at `.github/workflows/docker-publish.yml`.

It will automatically:

- build the Docker image on every push to `main`
- push the image to `ghcr.io`
- publish a multi-arch image for `linux/amd64` and `linux/arm64`
- publish `latest` for the default branch
- publish tag-based versions for Git tags like `v1.0.0`
- publish a `sha-...` image tag for traceability

The resulting image name is:

```text
ghcr.io/<owner>/<repo>
```

Examples:

```text
ghcr.io/matthias/unifiplus:latest
ghcr.io/matthias/unifiplus:v1.0.0
ghcr.io/matthias/unifiplus:sha-abcdef1
```

To publish it on GitHub:

1. Create a GitHub repository.
2. Add this project as the remote.
3. Push the repository to GitHub.
4. Make sure GitHub Packages is enabled for the repository.

Example:

```bash
git remote add origin git@github.com:<owner>/<repo>.git
git branch -M main
git push -u origin main
```

If you want to override the default images or ports locally, set:

```bash
export UNIFIPLUS_DEV_IMAGE=unifiplus-local:dev
export UNIFIPLUS_MASTER_IMAGE=ghcr.io/<owner>/<repo>:latest
export UNIFIPLUS_DEV_PORT=8080
export UNIFIPLUS_MASTER_PORT=8081
docker compose up -d
```

## REST API

UnifiPlus now includes a JSON REST API for desktop helpers and tray tools.

Authentication:

- create an API key in the account page
- send it as `X-API-Key: <key>` or `Authorization: Bearer <key>`

Endpoints:

- `GET /api/v1/health`
- `GET /api/v1/me`
- `GET /api/v1/wans`
- `GET /api/v1/clients`
- `GET /api/v1/clients?scope=all`
- `POST /api/v1/clients/{clientId}/claim`
- `POST /api/v1/clients/{clientId}/uplink`
- `POST /api/v1/clients/{clientId}/bandwidth`
- `GET /api/v1/bandwidth/templates`

Examples:

```bash
curl -H "X-API-Key: YOUR_KEY" http://localhost:8080/api/v1/me
curl -H "X-API-Key: YOUR_KEY" http://localhost:8080/api/v1/wans
curl -X POST http://localhost:8080/api/v1/clients/CLIENT_ID/uplink \
  -H "Content-Type: application/json" \
  -H "X-API-Key: YOUR_KEY" \
  -d '{"wanId":"wan2"}'
```

## Next implementation steps

1. Replace the demo login with real authentication.
2. Implement the real UniFi API adapter.
3. Persist device ownership and WAN rules through UniFi naming conventions.
4. Add authorization so users only control their own devices.
