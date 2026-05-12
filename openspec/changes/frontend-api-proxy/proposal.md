## Why

The Angular frontend calls backend APIs using relative paths (`/api/...`) but there is no proxy configured to forward those requests to the API service. In the Aspire dev environment, the API runs on a dynamically-assigned port so the frontend cannot hard-code the backend URL — it must be injected at startup.

## What Changes

- Add a `proxy.conf.js` to the Angular app that reads `API_URL` from the environment and proxies all `/api` requests to the backend.
- Configure `angular.json` to pass `--proxy-config proxy.conf.js` to `ng serve`.
- Update the Aspire AppHost to inject `API_URL` into the frontend process, pointing to the API HTTPS endpoint, and ensure the frontend waits for the API to be ready.
- Remove any hardcoded API base-URL references from Angular services (they already use relative `/api/` paths, so this should be a no-op verification step).

## Capabilities

### New Capabilities

- `frontend-api-proxy`: Angular dev-server proxy that reads `API_URL` from the environment and forwards all `/api` traffic to the backend API. Aspire injects the correct backend URL as an environment variable before the frontend process starts.

### Modified Capabilities

_(none)_

## Impact

- `src/App/proxy.conf.js` — new file
- `src/App/angular.json` — add `proxyConfig` to the `serve` builder options
- `src/App/package.json` — update `start:aspire` script if needed
- `src/card-cheesi.AppHost/AppHost.cs` — inject `API_URL` env var into frontend resource
- Angular services — verify they use relative `/api/` paths (no base-URL config needed)
