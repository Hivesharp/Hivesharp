import { MessageSquarePlus, MessageSquare } from 'lucide-react';
import type { Thread } from '@/types';
import { truncateId } from '@/utils';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';

interface ThreadListProps {
  threads: Thread[];
  selectedThreadId: string | null;
  onSelectThread: (threadId: string) => void;
  onNewThread: () => void;
}

function formatThreadTitle(thread: Thread): string {
  if (thread.title) return thread.title;
  return `Thread ${truncateId(thread.id)}`;
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return 'Just now';
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr}h ago`;
  const diffDay = Math.floor(diffHr / 24);
  return `${diffDay}d ago`;
}

export default function ThreadList({ threads, selectedThreadId, onSelectThread, onNewThread }: ThreadListProps) {
  return (
    <div className="flex flex-col">
      <Separator className="bg-sidebar-border" />
      <div className="px-4 pt-4 pb-2 flex items-center justify-between">
        <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">Threads</span>
        <Button variant="ghost" size="sm" className="h-6 w-6 p-0" onClick={onNewThread}>
          <MessageSquarePlus className="size-3.5" />
        </Button>
      </div>
      <ScrollArea className="flex-1 px-2">
        <div className="space-y-0.5 pb-2">
          {threads.map((thread) => (
            <button
              key={thread.id}
              onClick={() => onSelectThread(thread.id)}
              className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
                selectedThreadId === thread.id
                  ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                  : 'hover:bg-sidebar-accent/50 text-sidebar-foreground'
              }`}
            >
              <div className="flex items-center gap-2">
                <MessageSquare className="size-3.5 shrink-0 text-muted-foreground" />
                <span className="text-sm truncate">{formatThreadTitle(thread)}</span>
              </div>
              <div className="text-[10px] text-muted-foreground mt-0.5 ml-5.5">
                {formatDate(thread.createdAt)}
              </div>
            </button>
          ))}
          {threads.length === 0 && (
            <div className="text-xs text-muted-foreground text-center py-4">
              No conversations yet
            </div>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
