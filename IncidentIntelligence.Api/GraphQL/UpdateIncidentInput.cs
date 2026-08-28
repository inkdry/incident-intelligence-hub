using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Api.GraphQL;

public sealed record UpdateIncidentInput(Guid Id, string Title, string Description, IncidentSeverity Severity);
