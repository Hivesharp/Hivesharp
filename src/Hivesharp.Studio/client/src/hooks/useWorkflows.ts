import { useEffect, useState, useCallback } from 'react';
import type { WorkflowDescriptor, WorkflowExecutionResult, WorkflowRunSnapshot } from '@/types';
import { fetchWorkflows, executeWorkflow, fetchSuspendedRuns, resumeWorkflowRun } from '@/api';
import { toast } from '@/lib/toast';

export function useWorkflows() {
  const [workflows, setWorkflows] = useState<WorkflowDescriptor[]>([]);
  const [selectedWorkflowId, setSelectedWorkflowId] = useState<string | null>(null);
  const [runs, setRuns] = useState<Record<string, WorkflowExecutionResult[]>>({});
  const [suspendedRuns, setSuspendedRuns] = useState<Record<string, WorkflowRunSnapshot[]>>({});
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    fetchWorkflows().then(setWorkflows).catch(() => {
      toast.error('Failed to load workflows');
    });
  }, []);

  const loadSuspendedRuns = useCallback(async (workflowId: string) => {
    try {
      const snapshots = await fetchSuspendedRuns(workflowId);
      setSuspendedRuns((prev) => ({ ...prev, [workflowId]: snapshots }));
    } catch (e) {
      toast.error('Failed to load suspended runs');
    }
  }, []);

  useEffect(() => {
    if (selectedWorkflowId) {
      loadSuspendedRuns(selectedWorkflowId);
    }
  }, [selectedWorkflowId, loadSuspendedRuns]);

  const selectedWorkflow = workflows.find((w) => w.id === selectedWorkflowId) ?? null;
  const workflowRuns = selectedWorkflowId ? (runs[selectedWorkflowId] ?? []) : [];
  const workflowSuspendedRuns = selectedWorkflowId ? (suspendedRuns[selectedWorkflowId] ?? []) : [];

  const handleSelectWorkflow = useCallback((id: string) => {
    setSelectedWorkflowId(id);
  }, []);

  const addRunResult = useCallback((workflowId: string, result: WorkflowExecutionResult) => {
    setRuns((prev) => ({
      ...prev,
      [workflowId]: [result, ...(prev[workflowId] ?? [])],
    }));
  }, []);

  const handleExecute = useCallback(async (input: unknown) => {
    if (!selectedWorkflowId) return;
    setLoading(true);
    try {
      const result = await executeWorkflow(selectedWorkflowId, input);
      addRunResult(selectedWorkflowId, result);
      if (result.status === 'Suspended') {
        await loadSuspendedRuns(selectedWorkflowId);
      }
    } catch (e) {
      toast.error('Failed to execute workflow');
    } finally {
      setLoading(false);
    }
  }, [selectedWorkflowId, addRunResult, loadSuspendedRuns]);

  const handleResume = useCallback(async (runId: string, resumeData: unknown) => {
    if (!selectedWorkflowId) return;
    setLoading(true);
    try {
      const result = await resumeWorkflowRun(selectedWorkflowId, runId, resumeData);
      addRunResult(selectedWorkflowId, result);
      await loadSuspendedRuns(selectedWorkflowId);
    } catch (e) {
      toast.error('Failed to resume workflow');
    } finally {
      setLoading(false);
    }
  }, [selectedWorkflowId, addRunResult, loadSuspendedRuns]);

  return {
    workflows,
    selectedWorkflow,
    selectedWorkflowId,
    workflowRuns,
    workflowSuspendedRuns,
    loading,
    handleSelectWorkflow,
    handleExecute,
    handleResume,
  };
}
