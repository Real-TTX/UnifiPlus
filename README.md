# UnifiPlus

`UnifiPlus` is an ASP.NET Core web app for managing UniFi-based WAN switching policies per user and client device.

## MVP idea

- Users sign in to the web app.
- Users can see UniFi clients and assign devices to themselves.
- Device ownership is stored through UniFi metadata and naming conventions.
- A WAN control updates the matching UniFi policy in the background.

## Project structure

- `src/UnifiPlus.Web`: ASP.NET Core MVC app
- `docker-compose.yml`: local development container
- `src/UnifiPlus.Web/Dockerfile`: Docker build for the web app

## Run locally with Docker

Build and start the local development container:

```bash
docker compose up -d --build
```

This starts one local container:

- `unifiplus-dev`: local development build on [http://localhost:8080](http://localhost:8080)

Stop the container:

```bash
docker compose down
```

Reset local development data:

```bash
docker compose down -v
```

## Local configuration

The compose file provides development defaults:

- `ASPNETCORE_ENVIRONMENT=Development`
- `DataStorage__RootPath=/data/unifiplus`
- `UniFi__BaseUrl=https://unifi.local`
- `UniFi__ApiKey=change-me`
- `UniFi__Site=default`

You can override the local image name or port with environment variables:

```bash
export UNIFIPLUS_DEV_IMAGE=unifiplus-local:dev
export UNIFIPLUS_DEV_PORT=8080
docker compose up -d --build
```

On Windows PowerShell:

```powershell
$env:UNIFIPLUS_DEV_IMAGE = "unifiplus-local:dev"
$env:UNIFIPLUS_DEV_PORT = "8080"
docker compose up -d --build
```

## REST API

UnifiPlus includes a JSON REST API for desktop helpers and tray tools.

Authentication:

- Create an API key in the account page.
- Send it as `X-API-Key: <key>` or `Authorization: Bearer <key>`.

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
