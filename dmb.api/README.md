# Deo Bernal Portfolio

## Overview
Senior Full-Stack .NET Developer with 20+ years of experience.

## Tech Stack
- .NET Core Web API
- React
- SQL Server

## Features
- Portfolio API
- Dynamic frontend
- Clean architecture

## Run Locally

### Backend
dotnet run

### Frontend
npm start

# DMB Projects

This repository contains the `dmb.api` project. A `dmb.web` project is not present in this workspace; placeholder notes are included for it.

---

## dmb.api

- Path: `.` (project file: `dmb.api.csproj`)
- Target framework: .NET 10 (`net10.0`)
- C# language version: 14
- Project type: ASP.NET Core Web API (minimal hosting + controllers)
- Key files:
  - `Program.cs` - application startup, service registration, routing and endpoints
  - `Controllers/ProfileController.cs` - example API controller (`GET /api/profile`)
  - `dmb.api.csproj` - project file, contains package references (e.g. `Microsoft.AspNetCore.OpenApi`)
  - `appsettings.json` / `appsettings.Development.json` - configuration
  - `Properties/launchSettings.json` - Visual Studio launch profiles
  - `dmb.api.http` - HTTP request collection used by the IDE (optional)

- Dependencies:
  - `Microsoft.AspNetCore.OpenApi` (version referenced in project)
  - .NET 10 SDK to build and run

- Important runtime behavior:
  - Services must be registered before calling `builder.Build()` in `Program.cs`.
  - Controllers are registered using `builder.Services.AddControllers()` and are exposed via `app.MapControllers()`.
  - CORS policy `AllowAll` is configured in `Program.cs` and applied with `app.UseCors("AllowAll")`.
  - OpenAPI is added via `builder.Services.AddOpenApi()` and mapped with `app.MapOpenApi()` when in development.

- Exposed endpoints (examples):
  - `GET /api/profile` - returns profile information (from `ProfileController`).
  - `GET /weatherforecast` - minimal endpoint defined in `Program.cs`.

- How to build and run (CLI):
  1. Ensure .NET 10 SDK is installed: `dotnet --list-sdks`.
  2. From repository root run:
     ```
     dotnet build
     dotnet run --project dmb.api.csproj
     ```
  3. Open the app URL shown in the console or use the configured launch profile in Visual Studio.

- How to run in Visual Studio:
  - Open the solution or folder in Visual Studio 2026 and use the `dmb.api` launch profile in the Debug target selector.

- Notes / troubleshooting:
  - If you see `InvalidOperationException: The service collection cannot be modified because it is read-only`, ensure no calls that modify `builder.Services` occur after `builder.Build()`.
  - Keep service registration and DI configuration grouped before `Build()`.
  - If adding libraries that require registration, add them before `builder.Build()`.

---

## dmb.web (existing project details)

The sibling frontend project is present at `../dmb.web` (absolute path in this environment: `C:\github\DMBProfile\dmb.web`). It is a Create React App based project (uses `react-scripts`).

Key `package.json` highlights (from `../dmb.web/package.json`):

- `name`: `dmb.web`
- `version`: `0.1.0`
- `react`: `^19.2.5`
- `react-dom`: `^19.2.5`
- `react-scripts`: `5.0.1` (Create React App scripts)
- `axios`: `^1.15.0`
- Dev dependencies include TypeScript (`^6.0.2`) and `@types/*` packages.

Available npm scripts:

- `start`: `react-scripts start`  (development server)
- `build`: `react-scripts build`  (production build)
- `test`: `react-scripts test`
- `eject`: `react-scripts eject`

How to run the frontend locally:

1. Open a terminal and change to the frontend folder:

   `cd ../dmb.web`  (or `C:\github\DMBProfile\dmb.web`)

2. Install dependencies:

   `npm install`

3. Start the dev server:

   `npm start`

Note: `react-scripts start` typically launches on port 3000 by default. You can change it with the `PORT` environment variable, e.g. `set PORT=5173 && npm start` on Windows PowerShell/CMD.

Integration with `dmb.api`:

- During development host the frontend separately and call API endpoints at the API origin. Ensure `dmb.api`'s CORS policy allows the frontend origin (for example `http://localhost:3000`).
- To serve the frontend from the API in production, run `npm run build` in `dmb.web` and copy the output (or configure a build pipeline) to `dmb.api/wwwroot`, then enable static file hosting in `dmb.api` (`app.UseStaticFiles()` and SPA fallback as needed).

If you want, I can also:
- Add a short script to `dmb.api` to run both projects concurrently during development.
- Or scaffold a Vite-based frontend if you prefer a newer toolchain.
