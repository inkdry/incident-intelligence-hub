using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Incidents;

public sealed record ReportIncidentCommand(string Title, string Description, IncidentSeverity Severity);
