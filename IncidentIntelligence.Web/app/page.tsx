'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { Activity, AlertTriangle, ArrowRight, CircleCheck, Clock3, Pencil, Plus, RefreshCw, ShieldCheck } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Sheet, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Textarea } from '@/components/ui/textarea';
import { getIncidents, Incident, IncidentSeverity, reportIncident, transitionIncident, updateIncident } from '@/lib/graphql';

const severities: IncidentSeverity[] = ['LOW', 'MEDIUM', 'HIGH', 'CRITICAL'];
const nextAction = {
  REPORTED: { label: 'Start investigation', mutation: 'startIncidentInvestigation' },
  INVESTIGATING: { label: 'Mark mitigated', mutation: 'mitigateIncident' },
  MITIGATED: { label: 'Resolve incident', mutation: 'resolveIncident' },
} as const;

function humanize(value: string) { return value.charAt(0) + value.slice(1).toLowerCase(); }
function formatDate(value: string | null) {
  return value ? new Intl.DateTimeFormat('en', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : 'Not yet';
}
function statusTone(status: Incident['status']) {
  if (status === 'RESOLVED' || status === 'REVIEWED') return 'bg-emerald-100 text-emerald-800';
  if (status === 'MITIGATED') return 'bg-sky-100 text-sky-800';
  if (status === 'INVESTIGATING') return 'bg-amber-100 text-amber-900';
  return 'bg-slate-100 text-slate-700';
}
function severityTone(severity: IncidentSeverity) {
  if (severity === 'CRITICAL') return 'border-red-200 bg-red-50 text-red-700';
  if (severity === 'HIGH') return 'border-orange-200 bg-orange-50 text-orange-700';
  if (severity === 'MEDIUM') return 'border-amber-200 bg-amber-50 text-amber-700';
  return 'border-sky-200 bg-sky-50 text-sky-700';
}

export default function Home() {
  const [incidents, setIncidents] = useState<Incident[]>([]);
  const [selected, setSelected] = useState<Incident | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Incident | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true); setError(null);
    try { setIncidents(await getIncidents()); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Unable to load incidents.'); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { queueMicrotask(() => void load()); }, [load]);
  useEffect(() => {
    const context = document.modelContext;
    if (!context?.registerTool) return;
    const lifecycle = new AbortController();
    void Promise.resolve(context.registerTool({
      name: 'report_incident',
      title: 'Report incident',
      description: 'Report a new operational incident and add it to the visible incident queue.',
      inputSchema: {
        type: 'object',
        properties: {
          title: { type: 'string', minLength: 1, maxLength: 200 },
          description: { type: 'string', minLength: 1, maxLength: 4000 },
          severity: { type: 'string', enum: severities },
        },
        required: ['title', 'description', 'severity'],
        additionalProperties: false,
      },
      annotations: { readOnlyHint: false, untrustedContentHint: false },
      async execute(input) {
        if (!input || typeof input !== 'object') throw new Error('Incident details are required.');
        const value = input as Record<string, unknown>;
        if (typeof value.title !== 'string' || !value.title.trim()) throw new Error('A title is required.');
        if (typeof value.description !== 'string' || !value.description.trim()) throw new Error('A description is required.');
        if (!severities.includes(value.severity as IncidentSeverity)) throw new Error('Severity is invalid.');
        const created = await reportIncident({ title: value.title, description: value.description, severity: value.severity as IncidentSeverity });
        setIncidents((current) => [created, ...current]);
        setSelected(created);
        return { id: created.id, status: created.status };
      },
    }, { signal: lifecycle.signal })).catch(() => undefined);
    return () => lifecycle.abort();
  }, []);

  const counts = useMemo(() => ({
    active: incidents.filter((item) => !['RESOLVED', 'REVIEWED'].includes(item.status)).length,
    critical: incidents.filter((item) => item.severity === 'CRITICAL' && !['RESOLVED', 'REVIEWED'].includes(item.status)).length,
    resolved: incidents.filter((item) => item.status === 'RESOLVED').length,
  }), [incidents]);

  function replaceIncident(updated: Incident) {
    setIncidents((current) => current.map((item) => item.id === updated.id ? updated : item));
    setSelected(updated);
  }
  async function handleTransition() {
    if (!selected || !(selected.status in nextAction)) return;
    setSaving(true); setError(null);
    try {
      const action = nextAction[selected.status as keyof typeof nextAction];
      replaceIncident(await transitionIncident(action.mutation, selected.id));
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Unable to update the incident.'); }
    finally { setSaving(false); }
  }
  async function saveIncident(values: { title: string; description: string; severity: IncidentSeverity }) {
    setSaving(true); setError(null);
    try {
      if (editing) replaceIncident(await updateIncident({ id: editing.id, ...values }));
      else {
        const created = await reportIncident(values);
        setIncidents((current) => [created, ...current]); setSelected(created);
      }
      setFormOpen(false);
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Unable to save the incident.'); }
    finally { setSaving(false); }
  }

  return <main className="min-h-screen bg-background">
    <header className="border-b border-white/10 bg-[#07111f] text-white">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-5 lg:px-8">
        <div className="flex items-center gap-3"><span className="grid size-10 place-items-center rounded-xl bg-cyan-400 text-[#07111f]"><ShieldCheck /></span><div><p className="text-lg font-semibold tracking-tight">Incident Intelligence</p><p className="text-xs text-slate-400">Operations command center</p></div></div>
        <Button onClick={() => { setEditing(null); setFormOpen(true); }} className="h-10 bg-cyan-400 px-4 text-[#07111f] hover:bg-cyan-300"><Plus /> Report incident</Button>
      </div>
    </header>

    <section className="mx-auto max-w-7xl px-5 py-8 lg:px-8">
      <div className="mb-8 flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div><p className="mb-2 text-sm font-medium text-cyan-700">LIVE OPERATIONS</p><h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Incident overview</h1><p className="mt-2 text-muted-foreground">Track every incident from first report through resolution.</p></div>
        <Button variant="outline" onClick={() => void load()} disabled={loading}><RefreshCw className={loading ? 'animate-spin' : ''} /> Refresh</Button>
      </div>
      <div className="mb-8 grid gap-4 sm:grid-cols-3">
        <Metric icon={<Activity />} label="Active incidents" value={counts.active} tone="cyan" />
        <Metric icon={<AlertTriangle />} label="Active critical" value={counts.critical} tone="red" />
        <Metric icon={<CircleCheck />} label="Resolved" value={counts.resolved} tone="green" />
      </div>
      {error && <div role="alert" className="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">{error}<button className="ml-2 font-semibold underline" onClick={() => setError(null)}>Dismiss</button></div>}
      <div className="overflow-hidden rounded-2xl border bg-card shadow-sm">
        <div className="flex items-center justify-between border-b px-5 py-4"><div><h2 className="font-semibold">Incident queue</h2><p className="text-sm text-muted-foreground">Newest reports appear first</p></div><Badge variant="outline">{incidents.length} total</Badge></div>
        {loading ? <div className="p-10 text-center text-muted-foreground">Loading incidents…</div> : incidents.length === 0 ?
          <div className="p-12 text-center"><CircleCheck className="mx-auto mb-3 size-10 text-emerald-500" /><h3 className="font-semibold">No incidents reported</h3><p className="mt-1 text-sm text-muted-foreground">Your incident queue is clear.</p><Button className="mt-5" onClick={() => { setEditing(null); setFormOpen(true); }}><Plus /> Report incident</Button></div> :
          <div className="divide-y">{incidents.map((incident) => <button key={incident.id} onClick={() => setSelected(incident)} className="grid w-full gap-3 px-5 py-4 text-left transition hover:bg-muted/60 sm:grid-cols-[minmax(0,1fr)_130px_140px_24px] sm:items-center">
            <div className="min-w-0"><p className="truncate font-medium">{incident.title}</p><p className="mt-1 flex items-center gap-1.5 text-sm text-muted-foreground"><Clock3 className="size-3.5" /> {formatDate(incident.reportedAtUtc)}</p></div>
            <Badge variant="outline" className={severityTone(incident.severity)}>{humanize(incident.severity)}</Badge><Badge className={statusTone(incident.status)}>{humanize(incident.status)}</Badge><ArrowRight className="hidden size-4 text-muted-foreground sm:block" />
          </button>)}</div>}
      </div>
    </section>

    <Sheet open={!!selected} onOpenChange={(open) => !open && setSelected(null)}><SheetContent className="w-full overflow-y-auto sm:max-w-xl">{selected && <>
      <SheetHeader className="border-b p-6 pr-14"><div className="mb-3 flex gap-2"><Badge variant="outline" className={severityTone(selected.severity)}>{humanize(selected.severity)}</Badge><Badge className={statusTone(selected.status)}>{humanize(selected.status)}</Badge></div><SheetTitle className="text-2xl leading-tight">{selected.title}</SheetTitle><SheetDescription>Reported {formatDate(selected.reportedAtUtc)}</SheetDescription></SheetHeader>
      <div className="space-y-7 p-6"><section><h3 className="mb-2 text-sm font-semibold uppercase tracking-wide text-muted-foreground">Description</h3><p className="leading-7">{selected.description}</p></section><section><h3 className="mb-4 text-sm font-semibold uppercase tracking-wide text-muted-foreground">Lifecycle</h3><Timeline label="Reported" value={selected.reportedAtUtc} active /><Timeline label="Investigation started" value={selected.investigationStartedAtUtc} active={!!selected.investigationStartedAtUtc} /><Timeline label="Mitigated" value={selected.mitigatedAtUtc} active={!!selected.mitigatedAtUtc} /><Timeline label="Resolved" value={selected.resolvedAtUtc} active={!!selected.resolvedAtUtc} last /></section></div>
      <SheetFooter className="border-t bg-muted/30 p-6"><Button variant="outline" onClick={() => { setEditing(selected); setFormOpen(true); }}><Pencil /> Edit details</Button>{selected.status in nextAction && <Button onClick={() => void handleTransition()} disabled={saving}>{saving ? 'Updating…' : nextAction[selected.status as keyof typeof nextAction].label}<ArrowRight /></Button>}</SheetFooter>
    </>}</SheetContent></Sheet>
    <IncidentForm key={editing?.id ?? 'new'} open={formOpen} incident={editing} saving={saving} onOpenChange={setFormOpen} onSave={saveIncident} />
  </main>;
}

function Metric({ icon, label, value, tone }: { icon: React.ReactNode; label: string; value: number; tone: 'cyan' | 'red' | 'green' }) {
  const colors = { cyan: 'bg-cyan-50 text-cyan-700', red: 'bg-red-50 text-red-700', green: 'bg-emerald-50 text-emerald-700' };
  return <div className="flex items-center gap-4 rounded-2xl border bg-card p-5 shadow-sm"><span className={`grid size-11 place-items-center rounded-xl ${colors[tone]}`}>{icon}</span><div><p className="text-2xl font-semibold">{value}</p><p className="text-sm text-muted-foreground">{label}</p></div></div>;
}

function Timeline({ label, value, active, last = false }: { label: string; value: string | null; active: boolean; last?: boolean }) {
  return <div className="flex gap-3"><div className="flex flex-col items-center"><span className={`mt-1 size-3 rounded-full ${active ? 'bg-cyan-500 ring-4 ring-cyan-100' : 'bg-slate-200'}`} />{!last && <span className="h-12 w-px bg-border" />}</div><div><p className={active ? 'font-medium' : 'text-muted-foreground'}>{label}</p><p className="mt-0.5 text-sm text-muted-foreground">{formatDate(value)}</p></div></div>;
}

function IncidentForm({ open, incident, saving, onOpenChange, onSave }: { open: boolean; incident: Incident | null; saving: boolean; onOpenChange: (open: boolean) => void; onSave: (values: { title: string; description: string; severity: IncidentSeverity }) => Promise<void> }) {
  const [severity, setSeverity] = useState<IncidentSeverity>(incident?.severity ?? 'MEDIUM');
  async function submit(form: HTMLFormElement) { const data = new FormData(form); const title = data.get('title'); const description = data.get('description'); if (typeof title !== 'string' || typeof description !== 'string') return; await onSave({ title, description, severity }); }
  return <Dialog open={open} onOpenChange={onOpenChange}><DialogContent className="sm:max-w-lg"><form onSubmit={(event) => { event.preventDefault(); void submit(event.currentTarget); }}><DialogHeader><DialogTitle>{incident ? 'Edit incident' : 'Report an incident'}</DialogTitle><DialogDescription>{incident ? 'Update the incident details and severity.' : 'Capture enough context for the response team to begin.'}</DialogDescription></DialogHeader><div className="space-y-5 py-6"><div className="space-y-2"><Label htmlFor="title">Title</Label><Input id="title" name="title" required maxLength={200} defaultValue={incident?.title} placeholder="Payment API unavailable" /></div><div className="space-y-2"><Label htmlFor="description">Description</Label><Textarea id="description" name="description" required maxLength={4000} defaultValue={incident?.description} placeholder="Describe the customer impact and current symptoms." className="min-h-28" /></div><div className="space-y-2"><Label htmlFor="severity">Severity</Label><Select value={severity} onValueChange={(value) => value && setSeverity(value as IncidentSeverity)}><SelectTrigger id="severity" className="w-full"><SelectValue /></SelectTrigger><SelectContent>{severities.map((item) => <SelectItem key={item} value={item}>{humanize(item)}</SelectItem>)}</SelectContent></Select></div></div><DialogFooter><Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button><Button type="submit" disabled={saving}>{saving ? 'Saving…' : incident ? 'Save changes' : 'Report incident'}</Button></DialogFooter></form></DialogContent></Dialog>;
}
