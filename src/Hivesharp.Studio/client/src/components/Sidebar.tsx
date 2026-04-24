import { AlertTriangle, Bot, Cpu, Database, GitBranch, Plug } from 'lucide-react';
import type { Agent, McpServerInfo, RagPipelineInfo, Thread, WorkflowDescriptor } from '@/types';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import ThreadList from './ThreadList';

interface SidebarProps {
  agents: Agent[];
  selectedAgentId: string | null;
  onSelectAgent: (id: string) => void;
  threads: Thread[];
  selectedThreadId: string | null;
  onSelectThread: (threadId: string) => void;
  onNewThread: () => void;
  workflows: WorkflowDescriptor[];
  selectedWorkflowId: string | null;
  onSelectWorkflow: (id: string) => void;
  mcpServers: McpServerInfo[];
  selectedMcpServerName: string | null;
  onSelectMcpServer: (name: string) => void;
  pipelines: RagPipelineInfo[];
  selectedPipelineId: string | null;
  onSelectPipeline: (id: string) => void;
}

export default function Sidebar({
  agents,
  selectedAgentId,
  onSelectAgent,
  threads,
  selectedThreadId,
  onSelectThread,
  onNewThread,
  workflows,
  selectedWorkflowId,
  mcpServers,
  selectedMcpServerName,
  onSelectMcpServer,
  pipelines,
  selectedPipelineId,
  onSelectPipeline,
  onSelectWorkflow,
}: SidebarProps) {
  const selectedAgent = agents.find((a) => a.id === selectedAgentId);

  return (
    <div className="w-72 bg-sidebar text-sidebar-foreground flex flex-col shrink-0 border-r border-sidebar-border">
      <div className="px-5 py-4 flex items-center gap-2.5">
        <div className="size-8 bg-primary rounded-lg flex items-center justify-center">
          <Cpu className="size-4 text-primary-foreground" />
        </div>
        <span className="text-base font-semibold tracking-tight">Hivesharp Studio</span>
      </div>
      <Separator className="bg-sidebar-border" />

      <ScrollArea className="flex-1">
        {/* Agents section */}
        <div className="px-4 pt-4 pb-2">
          <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">Agents</span>
        </div>

        <div className="px-2 space-y-1 pb-2">
          {agents.map((agent) => (
            <button
              key={agent.id}
              onClick={() => onSelectAgent(agent.id)}
              className={`w-full text-left px-3 py-2.5 rounded-lg transition-colors ${
                selectedAgentId === agent.id && !selectedWorkflowId && !selectedPipelineId && !selectedMcpServerName
                  ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                  : 'hover:bg-sidebar-accent/50 text-sidebar-foreground'
              }`}
            >
              <div className="flex items-center gap-2">
                <Bot className="size-4 shrink-0 text-muted-foreground" />
                <span className="font-medium text-sm truncate">{agent.id}</span>
              </div>
              <div className="text-xs text-muted-foreground mt-1 ml-6">{agent.model}</div>
              {agent.toolNames.length > 0 && (
                <div className="flex flex-wrap gap-1 mt-1.5 ml-6">
                  {agent.toolNames.map((name) => (
                    <Badge key={name} variant="secondary" className="text-[10px] h-4 px-1.5">
                      {name}
                    </Badge>
                  ))}
                </div>
              )}
              {agent.mcpServers?.length > 0 && (
                <div className="flex items-center gap-1 mt-1.5 ml-6">
                  <Plug className="size-3 text-muted-foreground" />
                  <span className="text-[10px] text-muted-foreground">
                    {agent.mcpServers.length} MCP {agent.mcpServers.length === 1 ? 'server' : 'servers'}
                  </span>
                </div>
              )}
            </button>
          ))}
        </div>

        {selectedAgent?.hasMemory && !selectedWorkflowId && !selectedPipelineId && !selectedMcpServerName && (
          <ThreadList
            threads={threads}
            selectedThreadId={selectedThreadId}
            onSelectThread={onSelectThread}
            onNewThread={onNewThread}
          />
        )}

        {/* Workflows section */}
        {workflows.length > 0 && (
          <>
            <Separator className="bg-sidebar-border mx-4" />
            <div className="px-4 pt-4 pb-2">
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">Workflows</span>
            </div>

            <div className="px-2 space-y-1 pb-2">
              {workflows.map((wf) => (
                <button
                  key={wf.id}
                  onClick={() => onSelectWorkflow(wf.id)}
                  className={`w-full text-left px-3 py-2.5 rounded-lg transition-colors ${
                    selectedWorkflowId === wf.id
                      ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                      : 'hover:bg-sidebar-accent/50 text-sidebar-foreground'
                  }`}
                >
                  <div className="flex items-center gap-2">
                    <GitBranch className="size-4 shrink-0 text-muted-foreground" />
                    <span className="font-medium text-sm truncate">{wf.id}</span>
                  </div>
                  <div className="text-xs text-muted-foreground mt-1 ml-6">
                    {wf.stepIds.length} steps
                  </div>
                </button>
              ))}
            </div>
          </>
        )}

        {/* MCP Servers section */}
        {mcpServers.length > 0 && (
          <>
            <Separator className="bg-sidebar-border mx-4" />
            <div className="px-4 pt-4 pb-2">
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">MCP Servers</span>
            </div>

            <div className="px-2 space-y-1 pb-2">
              {mcpServers.map((server) => (
                <button
                  key={server.name}
                  onClick={() => onSelectMcpServer(server.name)}
                  className={`w-full text-left px-3 py-2.5 rounded-lg transition-colors ${
                    selectedMcpServerName === server.name
                      ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                      : 'hover:bg-sidebar-accent/50 text-sidebar-foreground'
                  }`}
                >
                  <div className="flex items-center gap-2">
                    <Plug className="size-4 shrink-0 text-muted-foreground" />
                    <span className="font-medium text-sm truncate">{server.name}</span>
                  </div>
                  <div className="flex gap-1.5 mt-1 ml-6 items-center">
                    <Badge variant="outline" className="text-[10px] h-4 px-1.5 font-mono">{server.transportType}</Badge>
                    {server.isAvailable
                      ? <Badge variant="secondary" className="text-[10px] h-4 px-1.5">{server.toolNames.length} tools</Badge>
                      : <AlertTriangle className="h-3 w-3 text-destructive" />
                    }
                  </div>
                </button>
              ))}
            </div>
          </>
        )}

        {/* Knowledge section */}
        {pipelines.length > 0 && (
          <>
            <Separator className="bg-sidebar-border mx-4" />
            <div className="px-4 pt-4 pb-2">
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">Knowledge</span>
            </div>

            <div className="px-2 space-y-1 pb-2">
              {pipelines.map((p) => (
                <button
                  key={p.indexName}
                  onClick={() => onSelectPipeline(p.indexName)}
                  className={`w-full text-left px-3 py-2.5 rounded-lg transition-colors ${
                    selectedPipelineId === p.indexName
                      ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                      : 'hover:bg-sidebar-accent/50 text-sidebar-foreground'
                  }`}
                >
                  <div className="flex items-center gap-2">
                    <Database className="size-4 shrink-0 text-muted-foreground" />
                    <span className="font-medium text-sm truncate">{p.indexName}</span>
                  </div>
                  <div className="flex gap-1.5 mt-1 ml-6">
                    <Badge variant="secondary" className="text-[10px] h-4 px-1.5">{p.dimensions}d</Badge>
                    <Badge variant="secondary" className="text-[10px] h-4 px-1.5">chunk {p.chunkSize}</Badge>
                  </div>
                </button>
              ))}
            </div>
          </>
        )}
      </ScrollArea>

      <Separator className="bg-sidebar-border" />
      <div className="px-5 py-3">
        <span className="text-xs text-muted-foreground">Hivesharp Framework</span>
      </div>
    </div>
  );
}
