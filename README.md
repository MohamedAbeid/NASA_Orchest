# Orchest — Local Setup (Windows)

Project: `web/` = React 19 + TypeScript + Vite PWA (sensor station), `server/` = .NET 10 Minimal API + SignalR + EF Core (SQL Server, auto fallback to InMemory).

## 1. Prerequisites
- Node.js 20 LTS or 22 LTS — https://nodejs.org
- .NET 10 SDK — https://dotnet.microsoft.com/download/dotnet/10.0
- Optional: SQL Server (LocalDB / Express / Developer). If it is missing, the API still runs and reports `storage: "InMemory"`; sensor measurements stay in the browser's localStorage.

## 2. Backend
Double-click `start-server.bat` or run:

```bat
cd server
dotnet restore
dotnet run
```

Listens on http://localhost:5054. Check http://localhost:5054/api/health.

To use SQL Server, edit `server\appsettings.json`:

```json
"Database": { "Provider": "SqlServer" },
"ConnectionStrings": {
  "Default": "Server=(localdb)\\MSSQLLocalDB;Database=Orchest;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Use `"Provider": "InMemory"` to force the in-memory store. Tables are created with `EnsureCreated()` on startup.

## 3. Frontend
Double-click `start-web.bat` or run:

```bat
cd web
npm ci
npm run dev
```

Open http://localhost:5173. `/api` and `/hub` are proxied to http://localhost:5054 by `vite.config.ts`, so the frontend works with or without the backend.

## 4. Offline / PWA on the same LAN
```bat
cd web
npm run build
npx vite preview --host --port 4173
```
Open `http://<PC-IP>:4173` from a phone on the same Wi-Fi. The service worker needs a secure context; `localhost` works, plain LAN HTTP usually does not, so for phone testing use the published HTTPS site or run a local HTTPS tunnel.

## 5. What runs where
- All sensor analysis (camera, voice, typing, depression indicators) runs in the browser. Nothing leaves the device.
- Measurements are stored in localStorage (`orchest.sensors.v1`) and synced to `/api/sensors` only when the backend is reachable.
- The face model is in `web/public/models/face_landmarker.task`; MediaPipe wasm is in `web/public/mediapipe/wasm`.

## 6. Useful endpoints
- `GET /api/health` — status, storage provider
- `GET/POST /api/sensors` — sensor measurements
- `GET /api/state`, `POST /api/telemetry`, `POST /api/symptoms`, `GET /api/reports`
- SignalR hub: `/hub/health`
