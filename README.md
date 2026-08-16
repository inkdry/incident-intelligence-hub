# Incident Intelligence Hub

An AI-powered incident management platform built with React, GraphQL,
ASP.NET Core, and Azure.

## Overview

Incident Intelligence Hub helps teams record, investigate, and resolve
operational incidents. The platform will use AI to summarize incidents,
identify similar historical events, and generate draft post-incident reports.

## Current Features

- ASP.NET Core 10 GraphQL API
- Hot Chocolate GraphQL server
- Nitro GraphQL development interface
- Incident domain model
- Incident severity and lifecycle status
- xUnit v3 domain tests

## Planned Features

- GraphQL queries, mutations, and subscriptions
- SQL Server persistence with Entity Framework Core
- React and TypeScript user interface
- Incident timelines and corrective actions
- AI-generated incident summaries
- Similar-incident semantic search
- Authentication and role-based authorization
- Automated testing and GitHub Actions
- Azure deployment and observability

## Technology Stack

- .NET 10
- ASP.NET Core
- Hot Chocolate GraphQL
- React
- TypeScript
- SQL Server
- Entity Framework Core
- xUnit v3
- Azure

## Running the GraphQL API

1. Open `IncidentIntelligenceHub.slnx` in Visual Studio.
2. Set `IncidentIntelligence.Api` as the startup project.
3. Run the application.
4. Open `/graphql` using the HTTPS address displayed by Visual Studio.

Run this query in the Nitro GraphQL interface:

```graphql
query {
  status
}
```

## Project Status

This project is under active development as part of a professional
AI-engineering portfolio.