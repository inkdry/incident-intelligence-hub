using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Api.GraphQL;

public sealed record ReportIncidentInput(string Title, string Description, IncidentSeverity Severity);
