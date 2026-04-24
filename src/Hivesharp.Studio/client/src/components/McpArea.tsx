import { useState } from 'react';
import type { McpServerInfo } from '@/types';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { AlertTriangle, Bot, ChevronDown, ChevronRight, Plug, Wrench } from 'lucide-react';

interface McpAreaProps {
  server: McpServerInfo | null;
}

export default function McpArea({ server }: McpAreaProps) {
  const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set(['tools', 'agents']));

  if (!server) {
    return (
      <div className="flex-1 flex items-center justify-center text-muted-foreground">
        Select an MCP server to view details
      </div>
    );
  }

  const toggleSection = (section: string) => {
    setExpandedSections((prev) => {
      const next = new Set(prev);
      if (next.has(section)) next.delete(section);
      else next.add(section);
      return next;
    });
  };

  return (
    <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
      {/* Header */}
      <div className="shrink-0 px-6 py-4 border-b border-border">
        <div className="flex items-center gap-3">
          <div className="size-10 bg-primary/10 rounded-lg flex items-center justify-center">
            <Plug className="size-5 text-primary" />
          </div>
          <div>
            <h2 className="font-semibold text-lg">{server.name}</h2>
            <div className="flex items-center gap-2 mt-0.5">
              <Badge variant="outline" className="font-mono text-xs">
                {server.transportType}
              </Badge>
              {server.isAvailable
                ? <span className="text-xs text-muted-foreground">
                    {server.toolNames.length} {server.toolNames.length === 1 ? 'tool' : 'tools'}
                  </span>
                : <span className="flex items-center gap-1 text-xs text-destructive">
                    <AlertTriangle className="h-3 w-3" />
                    unavailable
                  </span>
              }
            </div>
          </div>
        </div>
      </div>

      <ScrollArea className="flex-1 min-h-0">
        <div className="px-6 py-4 space-y-4">
          {/* Tools section */}
          {!server.isAvailable ? (
            <div className="rounded-md border border-destructive/50 bg-destructive/10 p-3 flex gap-2 items-start">
              <AlertTriangle className="h-4 w-4 text-destructive mt-0.5 shrink-0" />
              <div>
                <p className="text-sm font-medium text-destructive">Server unavailable</p>
                {server.unavailableReason && (
                  <p className="text-xs text-muted-foreground mt-1 font-mono">{server.unavailableReason}</p>
                )}
              </div>
            </div>
          ) : (
            <Collapsible open={expandedSections.has('tools')} onOpenChange={() => toggleSection('tools')}>
              <CollapsibleTrigger className="w-full text-left flex items-center gap-2 py-2">
                {expandedSections.has('tools') ? (
                  <ChevronDown className="size-4 text-muted-foreground" />
                ) : (
                  <ChevronRight className="size-4 text-muted-foreground" />
                )}
                <Wrench className="size-4 text-muted-foreground" />
                <span className="text-sm font-medium">Tools</span>
                <Badge variant="secondary" className="ml-auto text-xs">
                  {server.toolNames.length}
                </Badge>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <div className="ml-6 mt-1 space-y-1">
                  {server.toolNames.length === 0 ? (
                    <p className="text-sm text-muted-foreground py-2">No tools discovered</p>
                  ) : (
                    server.toolNames.map((tool) => (
                      <div
                        key={tool}
                        className="flex items-center gap-2 px-3 py-2 rounded-md border border-border bg-card"
                      >
                        <span className="text-sm font-mono">{tool}</span>
                      </div>
                    ))
                  )}
                </div>
              </CollapsibleContent>
            </Collapsible>
          )}

          <Separator />

          {/* Agents section */}
          <Collapsible open={expandedSections.has('agents')} onOpenChange={() => toggleSection('agents')}>
            <CollapsibleTrigger className="w-full text-left flex items-center gap-2 py-2">
              {expandedSections.has('agents') ? (
                <ChevronDown className="size-4 text-muted-foreground" />
              ) : (
                <ChevronRight className="size-4 text-muted-foreground" />
              )}
              <Bot className="size-4 text-muted-foreground" />
              <span className="text-sm font-medium">Used by Agents</span>
              <Badge variant="secondary" className="ml-auto text-xs">
                {server.agentIds.length}
              </Badge>
            </CollapsibleTrigger>
            <CollapsibleContent>
              <div className="ml-6 mt-1 space-y-1">
                {server.agentIds.map((agentId) => (
                  <div
                    key={agentId}
                    className="flex items-center gap-2 px-3 py-2 rounded-md border border-border bg-card"
                  >
                    <Bot className="size-3.5 text-muted-foreground" />
                    <span className="text-sm font-mono">{agentId}</span>
                  </div>
                ))}
              </div>
            </CollapsibleContent>
          </Collapsible>
        </div>
      </ScrollArea>
    </div>
  );
}
