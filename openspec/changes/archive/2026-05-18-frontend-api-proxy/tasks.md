## 1. Angular Proxy Configuration

- [x] 1.1 Create `src/App/proxy.conf.js` that reads `process.env.API_URL` (falling back to `http://localhost:5266`) and exports an http-proxy-middleware config forwarding `/api` to that target with `changeOrigin: true`
- [x] 1.2 Update `angular.json` to add `"proxyConfig": "proxy.conf.js"`

## 2. Aspire AppHost Integration

- [x] 2.1 Update `AppHost.cs` to inject `API_URL`
- [x] 2.2 Verify the frontend resource already has `.WaitFor(api)` (add if missing)

## 3. Verification

- [x] 3.1 Confirm Angular services use relative `/api/...` paths only — no hard-coded host/port
- [x] 3.2 Run `ng build` to ensure no TypeScript/build errors introduced
- [x] 3.3 Restart the Aspire frontend resource and confirm it starts healthy with the proxy active
