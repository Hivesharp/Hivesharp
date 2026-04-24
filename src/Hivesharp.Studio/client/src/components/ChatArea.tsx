import { useEffect, useRef } from 'react';
import { Bot } from 'lucide-react';
import type { Agent, ChatMessage as ChatMessageType } from '@/types';
import { truncateId } from '@/utils';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Badge } from '@/components/ui/badge';
import ChatMessage from './ChatMessage';
import ChatInput from './ChatInput';
import LoadingDots from './LoadingDots';

interface ChatAreaProps {
  agent: Agent | null;
  messages: ChatMessageType[];
  onSend: (message: string) => void;
  loading: boolean;
  threadId?: string | null;
}

export default function ChatArea({ agent, messages, onSend, loading, threadId }: ChatAreaProps) {
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  if (!agent) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center gap-3 text-muted-foreground">
        <div className="size-12 rounded-2xl bg-muted flex items-center justify-center">
          <Bot className="size-6" />
        </div>
        <div className="text-center">
          <p className="text-base font-medium">No agent selected</p>
          <p className="text-sm mt-1">Choose an agent from the sidebar to start chatting</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 flex flex-col min-h-0">
      {/* Header */}
      <div className="px-6 py-3 flex items-center gap-3 border-b border-border bg-background">
        <div className="size-8 rounded-lg bg-primary/10 flex items-center justify-center">
          <Bot className="size-4 text-primary" />
        </div>
        <div className="flex-1 min-w-0">
          <h2 className="text-sm font-semibold leading-none">{agent.id}</h2>
          <p className="text-xs text-muted-foreground mt-1 truncate">
            {agent.instructions || agent.model}
          </p>
        </div>
        <div className="flex items-center gap-1.5 shrink-0">
          {threadId && (
            <Badge variant="secondary" className="text-[10px] font-mono">
              {truncateId(threadId)}
            </Badge>
          )}
          <Badge variant="outline" className="text-xs">
            {agent.model}
          </Badge>
        </div>
      </div>

      {/* Messages */}
      <ScrollArea className="flex-1 bg-muted/30">
        <div className="max-w-3xl mx-auto px-6 py-6 flex flex-col gap-4">
          {messages.length === 0 && (
            <div className="text-center text-muted-foreground text-sm py-12">
              Send a message to start the conversation
            </div>
          )}
          {messages.map((msg, i) => (
            <ChatMessage key={i} message={msg} />
          ))}
          {loading && <LoadingDots />}
          <div ref={messagesEndRef} />
        </div>
      </ScrollArea>

      <ChatInput onSend={onSend} disabled={loading} agentName={agent.id} />
    </div>
  );
}
