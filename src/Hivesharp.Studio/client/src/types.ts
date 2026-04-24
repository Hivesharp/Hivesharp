export interface McpServerDescriptor {
  name: string;
  transportType: string;
}

export interface McpServerStatus {
  name: string;
  isAvailable: boolean;
  toolNames: string[];
  unavailableReason?: string;
}

export interface AgentRuntimeState {
  mcpServers: McpServerStatus[];
  lastInitializedAt: string | null;
}

export interface Agent {
  id: string;
  model: string;
  instructions: string | null;
  toolNames: string[];
  hasMemory: boolean;
  mcpServers: McpServerDescriptor[];
}

export interface ToolCall {
  toolName: string;
  arguments: Record<string, unknown>;
  result: unknown;
  isError: boolean;
}

export interface Usage {
  inputTokens: number | null;
  outputTokens: number | null;
  totalTokens: number | null;
}

export interface GenerateResponse {
  completion: string;
  threadId: string | null;
  toolCalls: ToolCall[];
  usage: Usage | null;
  runtimeState?: AgentRuntimeState;
}

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  toolCalls?: ToolCall[];
  usage?: Usage | null;
}

export interface Thread {
  id: string;
  resourceId: string | null;
  title: string | null;
  createdAt: string;
}

export interface ThreadMessage {
  role: string;
  content: string;
  createdAt: string;
}

export interface WorkflowNodeDescriptor {
  id: string;
  type: 'step' | 'branch' | 'merge' | 'parallel';
  label: string | null;
}

export interface WorkflowEdgeDescriptor {
  source: string;
  target: string;
  label: string | null;
}

export interface WorkflowDescriptor {
  id: string;
  stepIds: string[];
  nodes: WorkflowNodeDescriptor[];
  edges: WorkflowEdgeDescriptor[];
}

export interface WorkflowStepResult {
  stepId: string;
  status: 'Completed' | 'Failed' | 'Skipped' | 'Suspended';
  output: unknown;
  duration: number;
}

export interface WorkflowExecutionResult {
  status: 'Completed' | 'Failed' | 'Suspended';
  runId?: string;
  output: unknown;
  steps: WorkflowStepResult[];
  suspendedStepId?: string;
  suspendPayload?: unknown;
}

export interface WorkflowRunSnapshot {
  runId: string;
  workflowId: string;
  suspendedAtStepId: string;
  suspendPayload: unknown;
  createdAt: string;
}

export interface McpServerInfo {
  name: string;
  transportType: string;
  toolNames: string[];
  agentIds: string[];
  isAvailable: boolean;
  unavailableReason?: string;
}

export interface RagPipelineInfo {
  indexName: string;
  dimensions: number;
  chunkSize: number;
  chunkOverlap: number;
  vectorStoreBackend: string;
}

export interface VectorSearchResult {
  id: string;
  text: string;
  score: number;
  metadata: Record<string, unknown>;
}
