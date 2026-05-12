## Context

The Angular SPA makes all backend calls using relative paths (e.g. `/api/games`). In development, `ng serve` handles routing but has no proxy rule configured, so requests to `/api/...` return 404. The backend (Aspire project `api`) runs on a dynamically-assigned port, so a static proxy target cannot be committed to source control.

Aspire's `AddNpmApp` already injects arbitrary environment variables into the Node process before startup via `.WithEnvironment(...)`. The Angular `ng serve` dev server supports a JavaScript proxy-config file (`proxy.conf.js`) that can read `process.env.*` at startup time.

## Goals / Non-Goals

**Goals:**
- All `/api` requests from the Angular dev server are forwarded to the backend API.
- The backend URL is injected by Aspire as `API_URL` before the frontend npm process starts.
- No backend URL is hard-coded in source control.
- Angular services continue to use relative `/api/...` paths — no base-URL configuration is needed in app code.

**Non-Goals:**
- Production hosting / nginx configuration (out of scope for this change).
- HTTPS / certificate pinning in the proxy (dev environment only; the proxy may accept self-signed certs from the API).
- Proxy rules for non-API routes.

## Decisions

### Decision 1 — `proxy.conf.js` over `proxy.conf.json`

`proxy.conf.json` does not support dynamic values. A `.js` module can call `process.env.API_URL` at startup. The target is therefore resolved at runtime, picked up from the environment variable that Aspire injects.

**Alternative considered:** Hard-code `http://localhost:5266` in `proxy.conf.json`. Rejected because the port is assigned dynamically by Aspire's DCP and can change between runs.

### Decision 2 — Aspire injects `API_URL` via `WithEnvironment`

`WithEnvironment("API_URL", api.GetEndpoint("http"))` passes the Aspire-managed HTTP endpoint URL directly into the npm process's environment. This is idiomatic for Aspire resource injection and keeps the AppHost as the single source of truth for service addresses.

**Alternative considered:** Use Aspire's service-discovery env-var convention (`services__api__http__0`). Rejected because `ng serve` (Node) does not implement .NET service discovery, so a plain URL env var is clearer.

### Decision 3 — HTTP endpoint (not HTTPS) for the proxy target

The dev proxy runs inside the same machine and process boundary; TLS termination is not needed for loopback traffic. Using the HTTP endpoint avoids certificate validation errors in the Node `http-proxy-middleware` layer.

## Risks / Trade-offs

- **Env var not set at startup** → `proxy.conf.js` logs a warning and defaults to `http://localhost:5266`; proxy still starts but may fail requests if the API is on a different port. Mitigation: Aspire's `WaitFor(api)` ensures the API is running before the frontend starts, and `WithEnvironment` is always set.
- **CORS on the API** → With the proxy in place, browser requests never cross origins, so CORS is no longer required for dev. The API's CORS policy should still be configured for production deployments.
- **`ng serve` only** → The proxy is a dev-server feature. Production builds require a separate reverse proxy (e.g. nginx, Caddy); this is out of scope.

## Migration Plan

1. Add `proxy.conf.js` to `src/App/`.
2. Update `angular.json` serve configuration to add `"proxyConfig": "proxy.conf.js"`.
3. Update `start:aspire` npm script if the proxy-config flag is not already in `angular.json`.
4. Update AppHost to inject `API_URL`.
5. Restart Aspire — frontend will pick up the proxy on next start.

No rollback complexity; removing the `proxyConfig` entry in `angular.json` reverts the behaviour.
