# Incident Intelligence Hub

![Incident Intelligence Hub](docs/incident-intelligence-hub-github.png)

An incident management platform designed for AI-assisted workflows. This repository contains the
API, domain implementation, and React frontend. Azure deployment is a planned feature.

[![Build and Test](https://github.com/inkdry/incident-intelligence-hub/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/inkdry/incident-intelligence-hub/actions/workflows/build-and-test.yml)

## Overview

Incident Intelligence Hub helps teams record, investigate, and resolve
operational incidents. The platform will use AI to summarize incidents,
identify similar historical events, and generate draft post-incident reports.

## Current Features

- ASP.NET Core 10 GraphQL API (Hot Chocolate)
- GraphQL queries and mutations for incidents (status, list, report, update, start investigation, mitigate, resolve)
- Entity Framework Core persistence to SQL Server (Incidents table and migrations)
- Incident domain model (validation and lifecycle)
- xUnit v3 tests (unit and integration)
- React and TypeScript incident dashboard
- AI-generated incident summaries via local Ollama or OpenAI, saved as drafts with generation timestamps and stale-summary detection

Note: The Nitro (GraphQL tooling) interface is available in Development only.

## Planned Features

- AI similarity search
- Authentication and role-based authorization
- Expanded automation and CI/CD workflows targeting GitHub Actions and Azure

## Technology Stack
### Implemented

- .NET 10
- ASP.NET Core
- GraphQL (Hot Chocolate)
- SQL Server
- Entity Framework Core
- xUnit v3
- React
- TypeScript

### Planned

- Azure
- AI integration (summarization, similarity search)

## Running the GraphQL API

1. Open `IncidentIntelligenceHub.slnx` in Visual Studio.
2. Set `IncidentIntelligence.Api` as the startup project.
3. Run the application.
4. Open `/graphql` using the HTTPS address displayed by Visual Studio.

The GraphQL endpoint accepts POST requests for queries and mutations. The
GraphQL tooling (Nitro) is enabled only when running in the Development
environment.

## Running the Frontend

For everyday Windows startup, double-click `Start-Local.cmd` in the repository root, or run
`.\Start-Local.cmd` from a VS Code terminal. It checks dependencies and the configured Ollama model,
starts Ollama if needed, and opens separate API and dashboard terminals. It uses `npm.cmd` and a
process-only PowerShell execution-policy override; it does not change your system policy.
Existing listeners on the app ports are left running; restart them manually after code changes.
Open `http://localhost:3000` after the dashboard is ready. Stop the API and dashboard with Ctrl+C
in their windows; Ollama stays running. This launcher assumes the repository's default API ports.
Run `.\Start-Local.cmd -CheckOnly` to check prerequisites without starting services.
Install dependencies, download the Ollama model, trust the development HTTPS certificate, and apply
database migrations once using the setup instructions below before using the launcher.
The launcher reads development JSON and environment overrides; keep provider/model settings there,
and reserve user secrets for credentials.

1. Start the API using its HTTPS launch profile.
2. From `IncidentIntelligence.Web`, copy `.env.example` to `.env.local` if you need to override the API URL.
3. Run `npm install`, then `npm run dev`.
4. Open `http://localhost:3000`.

The development API allows browser requests from ports 3000 and 5173.

Run this query (POST) against `/graphql`:

```graphql
query {
  status
}
```
## Database Setup

The API uses SQL Server LocalDB during local development.

In Visual Studio, open:

```text
Tools → NuGet Package Manager → Package Manager Console
```

Set `IncidentIntelligence.Infrastructure` as the default project, then run:

```powershell
Update-Database
```

The development connection string is configured in
`IncidentIntelligence.Api/appsettings.Development.json`.

### VS Code database setup

From the repository root (`C:\source\repos\IncidentIntelligenceHub`), apply migrations:

```powershell
dotnet ef database update --project IncidentIntelligence.Infrastructure --startup-project IncidentIntelligence.Api
```

If the EF command is unavailable, install it with `dotnet tool install --global dotnet-ef`.
Stop the API with Ctrl+C before rebuilding or applying migrations if Windows reports locked build files.

## AI-generated incident summaries

The API defaults to **Ollama**, using the local `llama3.2:3b` model. The existing Generate summary
and Regenerate summary actions work with either provider; no additional database migration is needed
to switch providers. Only the incident's title, description, severity, status, and lifecycle timestamps
are sent to the selected provider. Ollama mode does not use the OpenAI API key or fall back to OpenAI.

### Local Ollama setup (Windows / VS Code)

1. Install and open [Ollama for Windows](https://ollama.com/download/windows).
   Open a new VS Code terminal after installation so it can find the `ollama` command.
2. Download the configured model (a one-time download of approximately 2 GB):

   ```powershell
   ollama pull llama3.2:3b
   ```

3. Leave Ollama running. The Windows app serves its API at `http://localhost:11434`.
   If the app is not running, start it or run `ollama serve` in a separate terminal.
4. Stop the Incident Intelligence API with Ctrl+C, then restart it from the repository root:

   ```powershell
   dotnet run --project IncidentIntelligence.Api --launch-profile https
   ```

5. Refresh the dashboard, open an incident, and select **Generate summary**.
   The first request may take longer while the model loads. The API allows up to five minutes.

These settings are already in `IncidentIntelligence.Api/appsettings.Development.json`:

```json
"AI": { "Provider": "Ollama" },
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "llama3.2:3b"
}
```

For a different local model, download it with `ollama pull <model>` and update `Ollama:Model`.
For example, `llama3.2:1b` is smaller but may produce less accurate summaries. Restart the API
after configuration changes. Use downloaded local models to keep inference on your machine;
Ollama cloud models or a remote `BaseUrl` are not local inference.

If you see **Cannot reach Ollama**, start Ollama and check `BaseUrl`. If the model is missing,
run the pull command above. Empty, malformed, or truncated output is rejected and any previous
draft is retained. Ollama summaries record `ollama/<model>` in the saved model metadata.

See the [Ollama Windows guide](https://docs.ollama.com/windows),
[chat API](https://docs.ollama.com/api/chat), and [model details](https://ollama.com/library/llama3.2:3b).

### Optional OpenAI provider

To switch back, set `AI:Provider` to `OpenAI` in development configuration (or use the
`AI__Provider` environment variable). The API calls the
[OpenAI Responses API](https://developers.openai.com/api/docs/guides/text) using `gpt-4.1-mini`
by default; change `OpenAI:Model` to select another model. Requests use `store: false`.
The API key stays on the server and is never sent to the dashboard.

1. Apply the database migrations above (including `AddIncidentSummary`).
2. Set your key in .NET user secrets from the repository root. Replace the placeholder locally;
   do not put the key in `appsettings*.json`, frontend environment files, or source control.

   ```powershell
   dotnet user-secrets set "OpenAI:ApiKey" "YOUR_OPENAI_API_KEY" --project IncidentIntelligence.Api
   ```

   For server environments, use the `OpenAI__ApiKey` environment variable instead.
3. Restart the API:

   ```powershell
   dotnet run --project IncidentIntelligence.Api --launch-profile https
   ```

4. Open an incident in the dashboard and select **Generate summary**. The draft is saved automatically
   and displayed for review. Refresh and reopen the incident to verify persistence.

**Regenerate summary** replaces the previous draft only after successful generation. Failures keep the
saved draft and display an error in the details panel. Editing incident details or advancing its status
flags the summary as out of date. Generation does not change the incident's lifecycle status or mark a
draft as approved. Review is a human reading step; approval and summary editing workflows are not included.

The GraphQL mutation is `generateIncidentSummary(id: UUID!)`. Its returned incident exposes `summary`,
`summaryGeneratedAtUtc`, `summaryModel`, and `summaryIsStale`; these fields are also available on incident queries.
The migration adds nullable summary columns and an incident version used to detect concurrent edits.
No API key is required to start the API or use the existing incident workflows.

Automated tests use fake generators and HTTP responses, so they require neither a running Ollama server
nor OpenAI credits. Run `dotnet test IncidentIntelligenceHub.slnx --configuration Release` to check the API,
summary persistence, error handling, and lifecycle behavior.

## Project Status

This project is under active development as part of a professional
AI-engineering portfolio.
