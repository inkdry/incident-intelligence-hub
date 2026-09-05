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

Note: The Nitro (GraphQL tooling) interface is available in Development only.

## Planned Features

- AI-generated incident summaries and similarity search
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

## Project Status

This project is under active development as part of a professional
AI-engineering portfolio.
