export type IncidentSeverity = 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
export type IncidentStatus = 'REPORTED' | 'INVESTIGATING' | 'MITIGATED' | 'RESOLVED' | 'REVIEWED';

export interface Incident {
  id: string;
  title: string;
  description: string;
  severity: IncidentSeverity;
  status: IncidentStatus;
  reportedAtUtc: string;
  investigationStartedAtUtc: string | null;
  mitigatedAtUtc: string | null;
  resolvedAtUtc: string | null;
}

interface GraphQlResponse<T> {
  data?: T;
  errors?: Array<{ message: string }>;
}

const endpoint = process.env.NEXT_PUBLIC_API_URL ?? 'https://localhost:7039/graphql';

async function execute<T>(query: string, variables?: Record<string, unknown>): Promise<T> {
  const response = await fetch(endpoint, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ query, variables }),
  });
  if (!response.ok) throw new Error(`The API returned ${response.status}.`);
  const result = (await response.json()) as GraphQlResponse<T>;
  if (result.errors?.length) throw new Error(result.errors[0].message);
  if (!result.data) throw new Error('The API returned no data.');
  return result.data;
}

const incidentFields = `id title description severity status reportedAtUtc investigationStartedAtUtc mitigatedAtUtc resolvedAtUtc`;

export async function getIncidents(): Promise<Incident[]> {
  const data = await execute<{ incidents: Incident[] }>(`query Incidents { incidents { ${incidentFields} } }`);
  return data.incidents;
}

export async function reportIncident(input: { title: string; description: string; severity: IncidentSeverity }): Promise<Incident> {
  const data = await execute<{ reportIncident: Incident }>(
    `mutation Report($input: ReportIncidentInput!) { reportIncident(input: $input) { ${incidentFields} } }`, { input });
  return data.reportIncident;
}

export async function updateIncident(input: { id: string; title: string; description: string; severity: IncidentSeverity }): Promise<Incident> {
  const data = await execute<{ updateIncident: Incident }>(
    `mutation Update($input: UpdateIncidentInput!) { updateIncident(input: $input) { ${incidentFields} } }`, { input });
  return data.updateIncident;
}

export async function transitionIncident(mutation: 'startIncidentInvestigation' | 'mitigateIncident' | 'resolveIncident', id: string): Promise<Incident> {
  const data = await execute<Record<string, Incident>>(
    `mutation Transition($id: UUID!) { ${mutation}(id: $id) { ${incidentFields} } }`, { id });
  return data[mutation];
}
