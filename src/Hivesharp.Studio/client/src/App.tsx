import { useMemo, useState } from 'react';
import { Toaster } from 'sonner';
import { TooltipProvider } from '@/components/ui/tooltip';
import Sidebar from '@/components/Sidebar';
import ChatArea from '@/components/ChatArea';
import AgentInfoPanel from '@/components/AgentInfoPanel';
import WorkflowArea from '@/components/WorkflowArea';
import KnowledgeArea from '@/components/KnowledgeArea';
import McpArea from '@/components/McpArea';
import LogPanel from '@/components/LogPanel';
import { useAgentChat } from '@/hooks/useAgentChat';
import { useWorkflows } from '@/hooks/useWorkflows';
import { useKnowledge } from '@/hooks/useKnowledge';
import type { McpServerInfo } from '@/types';

export default function App() {
  const agentChat = useAgentChat();
  const workflowState = useWorkflows();
  const knowledge = useKnowledge();
  const [selectedMcpServerName, setSelectedMcpServerName] = useState<string | null>(null);

  // Aggregate MCP servers from all agents, combining descriptor (static) with runtime status
  const mcpServers = useMemo<McpServerInfo[]>(() => {
    const serverMap = new Map<string, McpServerInfo>();
    for (const agent of agentChat.agents) {
      const runtimeState = agentChat.agentStatuses[agent.id];
      for (const mcp of agent.mcpServers ?? []) {
        const status = runtimeState?.mcpServers.find((s) => s.name === mcp.name);
        const existing = serverMap.get(mcp.name);
        if (existing) {
          if (!existing.agentIds.includes(agent.id)) {
            existing.agentIds.push(agent.id);
          }
        } else {
          serverMap.set(mcp.name, {
            name: mcp.name,
            transportType: mcp.transportType,
            toolNames: status?.toolNames ?? [],
            agentIds: [agent.id],
            isAvailable: status?.isAvailable ?? false,
            unavailableReason: status?.unavailableReason,
          });
        }
      }
    }
    return Array.from(serverMap.values());
  }, [agentChat.agents, agentChat.agentStatuses]);

  const selectedMcpServer = mcpServers.find((s) => s.name === selectedMcpServerName) ?? null;

  const handleSelectAgent = (id: string) => {
    workflowState.handleSelectWorkflow('');
    knowledge.selectPipeline(null);
    setSelectedMcpServerName(null);
    agentChat.handleSelectAgent(id);
  };

  const handleSelectWorkflow = (id: string) => {
    knowledge.selectPipeline(null);
    setSelectedMcpServerName(null);
    workflowState.handleSelectWorkflow(id);
  };

  const handleSelectPipeline = (id: string) => {
    workflowState.handleSelectWorkflow('');
    setSelectedMcpServerName(null);
    knowledge.selectPipeline(id);
  };

  const handleSelectMcpServer = (name: string) => {
    workflowState.handleSelectWorkflow('');
    knowledge.selectPipeline(null);
    setSelectedMcpServerName(name);
  };

  const showWorkflow = !!workflowState.selectedWorkflowId;
  const showKnowledge = !!knowledge.selectedPipeline;
  const showMcp = !!selectedMcpServer;

  return (
    <TooltipProvider>
      <Toaster position="bottom-right" />
      <div className="flex flex-col h-screen bg-background text-foreground">
        <div className="flex flex-1 min-h-0">
          <Sidebar
            agents={agentChat.agents}
            selectedAgentId={showWorkflow || showKnowledge || showMcp ? null : agentChat.selectedAgentId}
            onSelectAgent={handleSelectAgent}
            threads={agentChat.threads}
            selectedThreadId={agentChat.selectedThreadId}
            onSelectThread={agentChat.handleSelectThread}
            onNewThread={agentChat.handleNewThread}
            workflows={workflowState.workflows}
            selectedWorkflowId={workflowState.selectedWorkflowId}
            onSelectWorkflow={handleSelectWorkflow}
            mcpServers={mcpServers}
            selectedMcpServerName={showMcp ? selectedMcpServerName : null}
            onSelectMcpServer={handleSelectMcpServer}
            pipelines={knowledge.pipelines}
            selectedPipelineId={knowledge.selectedPipeline?.indexName ?? null}
            onSelectPipeline={handleSelectPipeline}
          />
          {showKnowledge ? (
            <KnowledgeArea
              pipeline={knowledge.selectedPipeline}
              searchResults={knowledge.searchResults}
              searching={knowledge.searching}
              ingesting={knowledge.ingesting}
              onSearch={knowledge.search}
              onIngest={knowledge.ingest}
            />
          ) : showMcp ? (
            <McpArea server={selectedMcpServer} />
          ) : showWorkflow ? (
            <WorkflowArea
              workflow={workflowState.selectedWorkflow}
              runs={workflowState.workflowRuns}
              suspendedRuns={workflowState.workflowSuspendedRuns}
              loading={workflowState.loading}
              onExecute={workflowState.handleExecute}
              onResume={workflowState.handleResume}
            />
          ) : (
            <>
              <main className="flex-1 flex flex-col min-w-0 min-h-0">
                <ChatArea
                  agent={agentChat.selectedAgent}
                  messages={agentChat.messages}
                  onSend={agentChat.handleSend}
                  loading={agentChat.loading}
                  threadId={agentChat.selectedThreadId}
                />
              </main>
              {agentChat.selectedAgent && (
                <AgentInfoPanel
                  agent={agentChat.selectedAgent}
                  runtimeState={agentChat.selectedAgentId ? agentChat.agentStatuses[agentChat.selectedAgentId] : undefined}
                  workingMemory={agentChat.workingMemory}
                  threadId={agentChat.selectedThreadId}
                  onRetryMcp={agentChat.selectedAgentId ? () => agentChat.handleRetryMcp(agentChat.selectedAgentId!) : undefined}
                />
              )}
            </>
          )}
        </div>
        <LogPanel />
      </div>
    </TooltipProvider>
  );
}
