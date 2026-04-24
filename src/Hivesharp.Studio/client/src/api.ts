import type { Agent, AgentRuntimeState, GenerateResponse, RagPipelineInfo, Thread, ThreadMessage, VectorSearchResult, WorkflowDescriptor, WorkflowExecutionResult, WorkflowRunSnapshot } from '@/types';
import { logStore } from '@/lib/log-store';

function getBasePath(): string {
  const base = document.querySelector('base')?.getAttribute('href');
  if (!base || base === '/') return '';
  return base.replace(/\/$/, '');
}

async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const method = init?.method ?? 'GET';
  const shortUrl = url.replace(getBasePath(), '');
  const start = performance.now();

  try {
    const res = await fetch(url, init);
    const duration = Math.round(performance.now() - start);

    if (!res.ok) {
      const text = await res.text().catch(() => res.statusText);
      let errorMessage = `${res.status}: ${text}`;
      try {
        const json = JSON.parse(text);
        if (json.error) errorMessage = json.error;
      } catch {
        // not JSON, keep raw
      }
      logStore.error(`${method} ${shortUrl}`, { status: res.status, duration, detail: errorMessage, method, url: shortUrl });
      throw new Error(errorMessage);
    }

    const data = await res.json();
    logStore.info(`${method} ${shortUrl}`, { status: res.status, duration, method, url: shortUrl });
    return data;
  } catch (e) {
    const duration = Math.round(performance.now() - start);
    if (e instanceof TypeError) {
      // Network error (fetch failed)
      logStore.error(`${method} ${shortUrl}`, { duration, detail: e.message, method, url: shortUrl });
    }
    throw e;
  }
}

export function fetchAgents(): Promise<Agent[]> {
  return fetchJson(`${getBasePath()}/api/agents`);
}

export function fetchAgentStatus(agentId: string): Promise<AgentRuntimeState> {
  return fetchJson(`${getBasePath()}/api/agents/${agentId}/status`);
}

export function generateMessage(agentId: string, message: string, threadId?: string | null): Promise<GenerateResponse> {
  return fetchJson(`${getBasePath()}/api/agents/${agentId}/generate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message, threadId: threadId ?? undefined }),
  });
}

export function createThread(agentId: string): Promise<Thread> {
  return fetchJson(`${getBasePath()}/api/agents/${agentId}/threads`, { method: 'POST' });
}

export function fetchThreads(agentId: string): Promise<Thread[]> {
  return fetchJson(`${getBasePath()}/api/agents/${agentId}/threads`);
}

export function fetchThreadMessages(agentId: string, threadId: string): Promise<ThreadMessage[]> {
  return fetchJson(`${getBasePath()}/api/agents/${agentId}/threads/${threadId}/messages`);
}

export function fetchWorkingMemory(agentId: string, threadId: string): Promise<{ content: string | null }> {
  return fetchJson(`${getBasePath()}/api/agents/${agentId}/threads/${threadId}/working-memory`);
}

export function retryMcp(agentId: string): Promise<AgentRuntimeState> {
  return fetchJson(`${getBasePath()}/api/agents/${agentId}/mcp/retry`, { method: 'POST' });
}

export function fetchWorkflows(): Promise<WorkflowDescriptor[]> {
  return fetchJson(`${getBasePath()}/api/workflows`);
}

export function executeWorkflow(workflowId: string, input: unknown): Promise<WorkflowExecutionResult> {
  return fetchJson(`${getBasePath()}/api/workflows/${workflowId}/execute`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ input }),
  });
}

export function fetchSuspendedRuns(workflowId: string): Promise<WorkflowRunSnapshot[]> {
  return fetchJson(`${getBasePath()}/api/workflows/${workflowId}/runs`);
}

export function resumeWorkflowRun(workflowId: string, runId: string, resumeData: unknown): Promise<WorkflowExecutionResult> {
  return fetchJson(`${getBasePath()}/api/workflows/${workflowId}/runs/${runId}/resume`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ resumeData }),
  });
}

export function fetchRagPipelines(): Promise<RagPipelineInfo[]> {
  return fetchJson(`${getBasePath()}/api/rag/pipelines`);
}

export function ingestDocument(indexName: string, content: string, source?: string, mimeType?: string): Promise<{ success: boolean }> {
  return fetchJson(`${getBasePath()}/api/rag/pipelines/${indexName}/ingest`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ content, source: source || undefined, mimeType: mimeType || undefined }),
  });
}

export function queryIndex(indexName: string, query: string, topK?: number): Promise<VectorSearchResult[]> {
  return fetchJson(`${getBasePath()}/api/rag/pipelines/${indexName}/query`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ query, topK }),
  });
}
