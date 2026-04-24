import { useState } from 'react';
import { ChevronUp, ChevronDown, Trash2, CircleX, CircleAlert, Info, Bug } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { useLogs } from '@/hooks/useLogs';
import type { LogLevel, LogEntry } from '@/lib/log-store';

const LEVEL_CONFIG: Record<LogLevel, { icon: typeof Info; color: string; bg: string }> = {
  info: { icon: Info, color: 'text-blue-500', bg: 'bg-blue-500/10' },
  error: { icon: CircleX, color: 'text-destructive', bg: 'bg-destructive/10' },
  warning: { icon: CircleAlert, color: 'text-amber-500', bg: 'bg-amber-500/10' },
  debug: { icon: Bug, color: 'text-muted-foreground', bg: 'bg-muted' },
};

function formatTime(date: Date): string {
  return date.toLocaleTimeString('en-GB', { hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

function LogRow({ entry }: { entry: LogEntry }) {
  const config = LEVEL_CONFIG[entry.level];
  const Icon = config.icon;

  return (
    <Collapsible>
      <CollapsibleTrigger className="flex items-center gap-2 w-full text-left px-3 py-1.5 hover:bg-muted/50 transition-colors text-xs font-mono cursor-pointer group">
        <span className="text-muted-foreground shrink-0 w-16">{formatTime(entry.timestamp)}</span>
        <Icon className={`size-3.5 shrink-0 ${config.color}`} />
        <span className="truncate flex-1 text-foreground/90">{entry.message}</span>
        {entry.status != null && (
          <Badge
            variant={entry.status >= 400 ? 'destructive' : 'secondary'}
            className="text-[10px] h-4 font-mono shrink-0"
          >
            {entry.status}
          </Badge>
        )}
        {entry.duration != null && (
          <span className="text-muted-foreground shrink-0 w-14 text-right">{entry.duration}ms</span>
        )}
      </CollapsibleTrigger>
      {entry.detail && (
        <CollapsibleContent>
          <pre className="text-[11px] font-mono text-foreground/70 bg-muted/50 mx-3 mb-1.5 px-3 py-2 rounded overflow-x-auto whitespace-pre-wrap max-h-32 overflow-y-auto">
            {entry.detail}
          </pre>
        </CollapsibleContent>
      )}
    </Collapsible>
  );
}

export default function LogPanel() {
  const { entries, clear } = useLogs();
  const [open, setOpen] = useState(false);
  const [filter, setFilter] = useState<LogLevel | 'all'>('all');

  const errorCount = entries.filter((e) => e.level === 'error').length;
  const filtered = filter === 'all' ? entries : entries.filter((e) => e.level === filter);

  return (
    <div className="border-t border-border bg-background flex flex-col shrink-0">
      {/* Toggle bar */}
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center gap-2 px-4 py-1.5 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors cursor-pointer"
      >
        {open ? <ChevronDown className="size-3.5" /> : <ChevronUp className="size-3.5" />}
        <span>Logs</span>
        {errorCount > 0 && (
          <Badge variant="destructive" className="text-[10px] h-4 font-mono">
            {errorCount}
          </Badge>
        )}
        <span className="text-muted-foreground/50">{entries.length} entries</span>
      </button>

      {/* Log content */}
      {open && (
        <div className="flex flex-col" style={{ height: 240 }}>
          {/* Toolbar */}
          <div className="flex items-center gap-1 px-3 py-1 border-t border-border">
            {(['all', 'error', 'warning', 'info', 'debug'] as const).map((level) => (
              <Button
                key={level}
                variant={filter === level ? 'secondary' : 'ghost'}
                size="sm"
                className="h-6 text-[10px] px-2"
                onClick={() => setFilter(level)}
              >
                {level === 'all' ? 'All' : level.charAt(0).toUpperCase() + level.slice(1)}
              </Button>
            ))}
            <div className="flex-1" />
            <Button variant="ghost" size="sm" className="h-6 text-[10px] px-2" onClick={clear}>
              <Trash2 className="size-3 mr-1" />
              Clear
            </Button>
          </div>

          {/* Entries */}
          <ScrollArea className="flex-1">
            {filtered.length === 0 ? (
              <div className="text-xs text-muted-foreground text-center py-8">No log entries</div>
            ) : (
              <div className="divide-y divide-border/50">
                {filtered.map((entry) => (
                  <LogRow key={entry.id} entry={entry} />
                ))}
              </div>
            )}
          </ScrollArea>
        </div>
      )}
    </div>
  );
}
