using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Incidents;

public sealed record UpdateIncidentCommand(Guid Id, string Title, string Description, IncidentSeverity Severity);
