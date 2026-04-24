import { useState } from 'react';
import type { LucideIcon } from 'lucide-react';
import { AlertTriangle, Cpu, Wrench, BrainCircuit, FileText, NotebookPen, Plug, RefreshCw } from 'lucide-react';
import type { Agent, AgentRuntimeState } from '@/types';
import { truncateId } from '@/utils';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';

function InfoSection({ icon: Icon, label, children }: { icon: LucideIcon; label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <div className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
        <Icon className="size-3" />
        {label}
      </div>
      {children}
    </div>
  );
}

interface AgentInfoPanelProps {
  agent: Agent;
  runtimeState?: AgentRuntimeState;
  workingMemory?: string | null;
  threadId?: string | null;
  onRetryMcp?: () => Promise<void>;
}

export default function AgentInfoPanel({ agent, runtimeState, workingMemory, threadId, onRetryMcp }: AgentInfoPanelProps) {
  const [retrying, setRetrying] = useState(false);

  const handleRetry = async () => {
    if (!onRetryMcp) return;
    setRetrying(true);
    try {
      await onRetryMcp();
    } finally {
      setRetrying(false);
    }
  };

  const hasFailed = runtimeState?.mcpServers.some((s) => !s.isAvailable) ?? false;

  return (
    <div className="w-64 bg-sidebar text-sidebar-foreground flex flex-col shrink-0 border-l border-sidebar-border">
      <div className="px-4 py-3 border-b border-sidebar-border">
        <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">Agent Info</span>
      </div>

      <div className="px-4 py-3 space-y-4 overflow-y-auto flex-1">
        <InfoSection icon={Cpu} label="Model">
          <Badge variant="outline" className="text-xs font-mono">
            {agent.model}
          </Badge>
        </InfoSection>

        <Separator className="bg-sidebar-border" />

        <InfoSection icon={BrainCircuit} label="Memory">
          <Badge variant={agent.hasMemory ? 'default' : 'secondary'} className="text-xs">
            {agent.hasMemory ? 'Enabled' : 'Disabled'}
          </Badge>
        </InfoSection>

        {agent.hasMemory && threadId && (
          <>
            <Separator className="bg-sidebar-border" />
            <InfoSection icon={NotebookPen} label="Working Memory">
              <Badge variant="outline" className="text-[10px] font-mono">
                Thread: ...{truncateId(threadId)}
              </Badge>
              {workingMemory ? (
                <pre className="text-xs text-foreground/80 leading-relaxed whitespace-pre-wrap bg-muted/50 rounded-md p-2 font-mono max-h-64 overflow-y-auto">
                  {workingMemory}
                </pre>
              ) : (
                <span className="text-xs text-muted-foreground italic">No working memory yet</span>
              )}
            </InfoSection>
          </>
        )}

        <Separator className="bg-sidebar-border" />

        <InfoSection icon={Wrench} label={`Tools (${agent.toolNames.length})`}>
          {agent.toolNames.length > 0 ? (
            <div className="flex flex-wrap gap-1">
              {agent.toolNames.map((name) => (
                <Badge key={name} variant="secondary" className="text-[10px] font-mono">
                  {name}
                </Badge>
              ))}
            </div>
          ) : (
            <span className="text-xs text-muted-foreground">No tools configured</span>
          )}
        </InfoSection>

        {agent.mcpServers?.length > 0 && (
          <>
            <Separator className="bg-sidebar-border" />
            <div className="space-y-1.5">
              <div className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
                <Plug className="size-3" />
                <span>MCP Servers ({agent.mcpServers.length})</span>
                {hasFailed && onRetryMcp && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-5 w-5 p-0 ml-auto"
                    onClick={handleRetry}
                    disabled={retrying}
                    title="Retry failed servers"
                  >
                    <RefreshCw className={`size-3 ${retrying ? 'animate-spin' : ''}`} />
                  </Button>
                )}
              </div>
              <div className="space-y-2">
                {agent.mcpServers.map((server) => {
                  const status = runtimeState?.mcpServers.find((s) => s.name === server.name);
                  const isAvailable = status?.isAvailable ?? false;
                  const toolNames = status?.toolNames ?? [];
                  return (
                    <Collapsible key={server.name}>
                      <CollapsibleTrigger className="flex items-center gap-1.5 text-xs hover:text-foreground transition-colors cursor-pointer w-full">
                        <Badge variant="outline" className="text-[10px] font-mono">
                          {server.transportType}
                        </Badge>
                        <span className="font-mono text-foreground/80">{server.name}</span>
                        {isAvailable
                          ? <span className="text-muted-foreground ml-auto">{toolNames.length} tools</span>
                          : <AlertTriangle className="h-3 w-3 text-destructive ml-auto" />
                        }
                      </CollapsibleTrigger>
                      <CollapsibleContent>
                        <div className="flex flex-wrap gap-1 mt-1.5 pl-2">
                          {toolNames.map((name) => (
                            <Badge key={name} variant="secondary" className="text-[10px] font-mono">
                              {name}
                            </Badge>
                          ))}
                        </div>
                      </CollapsibleContent>
                    </Collapsible>
                  );
                })}
              </div>
            </div>
          </>
        )}

        {agent.instructions && (
          <>
            <Separator className="bg-sidebar-border" />
            <InfoSection icon={FileText} label="Instructions">
              <Collapsible>
                <CollapsibleTrigger className="text-xs text-muted-foreground hover:text-foreground transition-colors cursor-pointer">
                  Show instructions
                </CollapsibleTrigger>
                <CollapsibleContent>
                  <p className="text-xs text-foreground/80 mt-1.5 leading-relaxed whitespace-pre-wrap">
                    {agent.instructions}
                  </p>
                </CollapsibleContent>
              </Collapsible>
            </InfoSection>
          </>
        )}
      </div>
    </div>
  );
}
