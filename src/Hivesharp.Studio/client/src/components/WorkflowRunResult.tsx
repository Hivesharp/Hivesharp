import type { WorkflowExecutionResult } from '@/types';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { CheckCircle2, XCircle, SkipForward, Pause, ChevronRight } from 'lucide-react';
import { useState } from 'react';

interface WorkflowRunResultProps {
  run: WorkflowExecutionResult;
  index: number;
}

function StatusIcon({ status }: { status: string }) {
  switch (status) {
    case 'Completed': return <CheckCircle2 className="size-4 text-green-500" />;
    case 'Failed': return <XCircle className="size-4 text-red-500" />;
    case 'Skipped': return <SkipForward className="size-4 text-muted-foreground" />;
    case 'Suspended': return <Pause className="size-4 text-yellow-500" />;
    default: return null;
  }
}

function formatDuration(ms: number): string {
  if (ms < 1) return '<1ms';
  if (ms < 1000) return `${Math.round(ms)}ms`;
  return `${(ms / 1000).toFixed(2)}s`;
}

export default function WorkflowRunResult({ run, index }: WorkflowRunResultProps) {
  const totalDuration = run.steps.reduce((acc, s) => acc + s.duration, 0);

  return (
    <Card className="p-4">
      <div className="flex items-center gap-2 mb-3">
        <StatusIcon status={run.status} />
        <span className="font-medium text-sm">Run #{index}</span>
        <Badge
          variant={run.status === 'Completed' ? 'default' : run.status === 'Suspended' ? 'outline' : 'destructive'}
          className={`text-xs ${run.status === 'Suspended' ? 'border-yellow-500/50 text-yellow-500' : ''}`}
        >
          {run.status}
        </Badge>
        <span className="text-xs text-muted-foreground ml-auto">{formatDuration(totalDuration)}</span>
      </div>

      {/* Steps */}
      <div className="space-y-1">
        {run.steps.map((step) => (
          <StepRow key={step.stepId} step={step} />
        ))}
      </div>

      {/* Suspend Payload */}
      {run.status === 'Suspended' && run.suspendPayload != null && (
        <div className="mt-3 pt-3 border-t border-yellow-500/20">
          <div className="text-xs font-medium text-yellow-500 mb-1">
            Suspended at: {run.suspendedStepId}
          </div>
          <pre className="text-xs font-mono bg-yellow-500/5 border border-yellow-500/20 rounded px-2 py-1.5 overflow-x-auto whitespace-pre-wrap">
            {JSON.stringify(run.suspendPayload, null, 2)}
          </pre>
        </div>
      )}

      {/* Output */}
      {run.output != null && (
        <div className="mt-3 pt-3 border-t border-border">
          <div className="text-xs font-medium text-muted-foreground mb-1">Output</div>
          <pre className="text-xs font-mono bg-muted rounded px-2 py-1.5 overflow-x-auto whitespace-pre-wrap">
            {typeof run.output === 'string' ? run.output : JSON.stringify(run.output, null, 2)}
          </pre>
        </div>
      )}
    </Card>
  );
}

function StepRow({ step }: { step: WorkflowExecutionResult['steps'][0] }) {
  const [open, setOpen] = useState(false);

  return (
    <Collapsible open={open} onOpenChange={setOpen}>
      <CollapsibleTrigger className="flex items-center gap-2 w-full text-left px-2 py-1.5 rounded hover:bg-muted/50 transition-colors">
        <ChevronRight className={`size-3 text-muted-foreground transition-transform ${open ? 'rotate-90' : ''}`} />
        <StatusIcon status={step.status} />
        <span className="text-sm font-mono">{step.stepId}</span>
        <span className="text-xs text-muted-foreground ml-auto">{formatDuration(step.duration)}</span>
      </CollapsibleTrigger>
      <CollapsibleContent>
        {step.output != null && (
          <pre className="text-xs font-mono bg-muted rounded px-3 py-2 ml-7 mt-1 mb-1 overflow-x-auto whitespace-pre-wrap">
            {typeof step.output === 'string' ? step.output : JSON.stringify(step.output, null, 2)}
          </pre>
        )}
      </CollapsibleContent>
    </Collapsible>
  );
}
