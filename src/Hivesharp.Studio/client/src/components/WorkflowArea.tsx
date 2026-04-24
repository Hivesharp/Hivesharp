import { useState } from 'react';
import type { WorkflowDescriptor, WorkflowExecutionResult, WorkflowRunSnapshot } from '@/types';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Play, RotateCcw } from 'lucide-react';
import WorkflowRunResult from './WorkflowRunResult';
import WorkflowGraph from './WorkflowGraph';

interface WorkflowAreaProps {
  workflow: WorkflowDescriptor | null;
  runs: WorkflowExecutionResult[];
  suspendedRuns: WorkflowRunSnapshot[];
  loading: boolean;
  onExecute: (input: unknown) => void;
  onResume: (runId: string, resumeData: unknown) => void;
}

export default function WorkflowArea({ workflow, runs, suspendedRuns, loading, onExecute, onResume }: WorkflowAreaProps) {
  const [inputText, setInputText] = useState('{}');
  const [inputError, setInputError] = useState<string | null>(null);
  const [resumeInputs, setResumeInputs] = useState<Record<string, string>>({});
  const [resumeErrors, setResumeErrors] = useState<Record<string, string>>({});

  if (!workflow) {
    return (
      <div className="flex-1 flex items-center justify-center text-muted-foreground">
        Select a workflow to get started
      </div>
    );
  }

  const handleRun = () => {
    try {
      const parsed = JSON.parse(inputText);
      setInputError(null);
      onExecute(parsed);
    } catch {
      setInputError('Invalid JSON');
    }
  };

  const handleResume = (runId: string) => {
    const text = resumeInputs[runId] || '{}';
    try {
      const parsed = JSON.parse(text);
      setResumeErrors((prev) => ({ ...prev, [runId]: '' }));
      onResume(runId, parsed);
    } catch {
      setResumeErrors((prev) => ({ ...prev, [runId]: 'Invalid JSON' }));
    }
  };

  const latestRun = runs.length > 0 ? runs[0] : undefined;

  return (
    <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
      {/* Header */}
      <div className="shrink-0 px-6 py-4 border-b border-border flex items-center gap-3">
        <h2 className="font-semibold text-lg">{workflow.id}</h2>
        <Badge variant="secondary">{workflow.stepIds.length} steps</Badge>
      </div>

      {/* Graph — takes all available space */}
      {workflow.nodes.length > 0 && (
        <div className="flex-1 min-h-0 border-b border-border">
          <WorkflowGraph workflow={workflow} stepResults={latestRun?.steps} />
        </div>
      )}

      {/* Bottom panel: Input + Suspended + Results */}
      <div className="shrink-0 flex flex-col max-h-[50%]">
        {/* Input & Run */}
        <div className="shrink-0 px-6 py-3 border-b border-border">
          <div className="flex gap-3 items-end">
            <div className="flex-1">
              <div className="text-xs font-medium text-muted-foreground uppercase tracking-wider mb-1.5">Input</div>
              <textarea
                value={inputText}
                onChange={(e) => { setInputText(e.target.value); setInputError(null); }}
                className="w-full bg-muted rounded-md px-3 py-2 text-sm font-mono resize-none border border-border focus:outline-none focus:ring-1 focus:ring-ring"
                rows={2}
                placeholder='{"key": "value"}'
              />
              {inputError && <p className="text-destructive text-xs mt-1">{inputError}</p>}
            </div>
            <Button onClick={handleRun} disabled={loading} size="default">
              <Play className="size-4 mr-1" />
              {loading ? 'Running...' : 'Run'}
            </Button>
          </div>
        </div>

        {/* Suspended Runs */}
        {suspendedRuns.length > 0 && (
          <div className="shrink-0 px-6 py-3 border-b border-border">
            <div className="text-xs font-medium text-muted-foreground uppercase tracking-wider mb-2">
              Suspended Runs ({suspendedRuns.length})
            </div>
            <div className="space-y-2">
              {suspendedRuns.map((snapshot) => (
                <div key={snapshot.runId} className="rounded-md border border-yellow-500/30 bg-yellow-500/5 p-3">
                  <div className="flex items-center gap-2 mb-2">
                    <Badge className="bg-yellow-500/20 text-yellow-400 border-yellow-500/30">Suspended</Badge>
                    <span className="text-xs text-muted-foreground font-mono">
                      {snapshot.runId.slice(0, 8)}...
                    </span>
                    <span className="text-xs text-muted-foreground">
                      at <span className="font-medium text-foreground">{snapshot.suspendedAtStepId}</span>
                    </span>
                  </div>
                  {snapshot.suspendPayload != null && (
                    <pre className="text-xs font-mono bg-muted rounded p-2 mb-2 overflow-x-auto max-h-20">
                      {JSON.stringify(snapshot.suspendPayload, null, 2)}
                    </pre>
                  )}
                  <div className="flex gap-2 items-end">
                    <div className="flex-1">
                      <textarea
                        value={resumeInputs[snapshot.runId] ?? '{}'}
                        onChange={(e) => {
                          setResumeInputs((prev) => ({ ...prev, [snapshot.runId]: e.target.value }));
                          setResumeErrors((prev) => ({ ...prev, [snapshot.runId]: '' }));
                        }}
                        className="w-full bg-muted rounded-md px-3 py-1.5 text-xs font-mono resize-none border border-border focus:outline-none focus:ring-1 focus:ring-ring"
                        rows={1}
                        placeholder='{"approved": true}'
                      />
                      {resumeErrors[snapshot.runId] && (
                        <p className="text-destructive text-xs mt-0.5">{resumeErrors[snapshot.runId]}</p>
                      )}
                    </div>
                    <Button
                      onClick={() => handleResume(snapshot.runId)}
                      disabled={loading}
                      size="sm"
                      variant="outline"
                      className="border-yellow-500/30 hover:bg-yellow-500/10"
                    >
                      <RotateCcw className="size-3 mr-1" />
                      Resume
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Results */}
        {runs.length > 0 && (
          <ScrollArea className="flex-1 min-h-0">
            <div className="px-6 py-3 space-y-3">
              {runs.map((run, i) => (
                <WorkflowRunResult key={i} run={run} index={runs.length - i} />
              ))}
            </div>
          </ScrollArea>
        )}
      </div>
    </div>
  );
}
