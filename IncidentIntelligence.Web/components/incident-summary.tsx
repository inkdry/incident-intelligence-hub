'use client';

import { useState } from 'react';
import { RefreshCw, Sparkles } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { generateIncidentSummary, type Incident } from '@/lib/graphql';

export function IncidentSummary({ incident, onGenerated }: { incident: Incident; onGenerated: (incident: Incident) => void }) {
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function generate() {
    if (generating) return;
    setGenerating(true);
    setError(null);
    try { onGenerated(await generateIncidentSummary(incident.id)); }
    catch (reason) { setError(reason instanceof Error ? reason.message : 'Unable to generate a summary. Please try again.'); }
    finally { setGenerating(false); }
  }

  return <section aria-labelledby="summary-heading" className="rounded-xl border border-cyan-200 bg-cyan-50/40 p-4">
    <h3 id="summary-heading" className="flex items-center gap-2 font-semibold"><Sparkles className="size-4 text-cyan-700" /> AI summary</h3>
    <p className="mt-1 text-sm text-muted-foreground">AI-generated draft. Review for accuracy before sharing.</p>
    {incident.summaryIsStale && <output className="mt-3 block rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">This incident has changed since the summary was generated. Regenerate it to include the latest details.</output>}
    <div aria-live="polite" aria-busy={generating}>
      {incident.summary ? <>
        <p className="mt-4 whitespace-pre-wrap break-words text-base leading-7">{incident.summary}</p>
        <p className="mt-3 text-sm text-muted-foreground">Saved {incident.summaryGeneratedAtUtc ? new Date(incident.summaryGeneratedAtUtc).toLocaleString() : ''}</p>
      </> : <p className="mt-4 text-sm text-muted-foreground">Generate a summary from this incident’s description, severity, and lifecycle.</p>}
    </div>
    {error && <p role="alert" className="mt-3 text-sm text-red-800">{error}</p>}
    <Button variant="outline" className="mt-4" disabled={generating} onClick={() => void generate()}>
      {generating ? <RefreshCw className="animate-spin" /> : <Sparkles />}
      {generating ? 'Generating summary…' : incident.summary ? 'Regenerate summary' : 'Generate summary'}
    </Button>
  </section>;
}
