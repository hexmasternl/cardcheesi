## ADDED Requirements

### Requirement: Angular dev server proxies /api traffic to the backend
The Angular `ng serve` dev server SHALL forward all HTTP requests whose path starts with `/api` to the URL specified by the `API_URL` environment variable.

#### Scenario: API request is proxied
- **WHEN** the Angular dev server receives a request to `/api/games`
- **THEN** the request is forwarded to `${API_URL}/api/games` and the response is returned to the browser

#### Scenario: Non-API request is not proxied
- **WHEN** the Angular dev server receives a request to `/some-page`
- **THEN** the request is handled by Angular's own router and not forwarded to the backend

#### Scenario: API_URL env var is absent
- **WHEN** the `API_URL` environment variable is not set at ng serve startup
- **THEN** the proxy SHALL fall back to `http://localhost:5266` and log a warning

### Requirement: Aspire injects the backend URL before the frontend starts
The Aspire AppHost SHALL inject `API_URL` as an environment variable into the frontend npm process, using the HTTP endpoint URL of the `api` resource.

#### Scenario: Aspire sets API_URL
- **WHEN** the Aspire AppHost starts the frontend resource
- **THEN** the `API_URL` environment variable in the npm process is set to the HTTP base URL of the `api` resource (e.g. `http://localhost:5266`)

#### Scenario: Frontend waits for the API
- **WHEN** the Aspire AppHost starts all resources
- **THEN** the frontend npm process MUST NOT start until the `api` resource is in the Running state

### Requirement: No hard-coded backend URL in source control
The Angular application source code and committed configuration files SHALL NOT contain a hard-coded backend host or port.

#### Scenario: Proxy target is resolved at runtime
- **WHEN** `proxy.conf.js` is loaded by ng serve
- **THEN** the proxy target is read from `process.env.API_URL`, not from a literal string in the file

#### Scenario: Angular services use relative paths
- **WHEN** an Angular service makes an HTTP request to the backend
- **THEN** the URL used SHALL be a relative path beginning with `/api/` with no host or port component
