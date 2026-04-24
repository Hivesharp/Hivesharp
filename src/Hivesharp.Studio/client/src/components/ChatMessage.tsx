import { Bot, User, CircleX } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Separator } from '@/components/ui/separator';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import type { ChatMessage as ChatMessageType } from '@/types';

interface ChatMessageProps {
  message: ChatMessageType;
}

function isErrorContent(content: string): boolean {
  return /^(Error:|System\.\w+Exception)/.test(content) ||
    content.includes('Exception:') ||
    content.includes('StatusCode:');
}

export default function ChatMessage({ message }: ChatMessageProps) {
  if (message.role === 'user') {
    return (
      <div className="flex items-end gap-2 justify-end">
        <div className="max-w-[70%] bg-primary text-primary-foreground px-4 py-2.5 rounded-2xl rounded-br-sm shadow-sm">
          <p className="text-sm leading-relaxed whitespace-pre-wrap">{message.content}</p>
        </div>
        <Avatar size="sm">
          <AvatarFallback className="bg-primary text-primary-foreground">
            <User className="size-3" />
          </AvatarFallback>
        </Avatar>
      </div>
    );
  }

  // Error message — inline alert style instead of chat bubble
  if (isErrorContent(message.content)) {
    return (
      <div className="flex items-start gap-2 w-full">
        <Avatar size="sm">
          <AvatarFallback className="bg-destructive/20">
            <CircleX className="size-3 text-destructive" />
          </AvatarFallback>
        </Avatar>
        <div className="flex-1 min-w-0">
          <div className="border-l-[3px] border-destructive bg-destructive/10 rounded-r-lg px-4 py-3">
            <div className="flex items-center gap-2 mb-1.5">
              <span className="text-xs font-semibold text-destructive uppercase tracking-wide">Error</span>
            </div>
            <pre className="text-xs font-mono text-foreground/80 whitespace-pre-wrap overflow-x-auto max-h-40 overflow-y-auto leading-relaxed">
              {message.content}
            </pre>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex items-start gap-2">
      <Avatar size="sm">
        <AvatarFallback className="bg-muted">
          <Bot className="size-3" />
        </AvatarFallback>
      </Avatar>
      <div className="max-w-[70%] space-y-2">
        <div className="bg-card border border-border px-4 py-2.5 rounded-2xl rounded-bl-sm shadow-sm">
          <p className="text-sm leading-relaxed whitespace-pre-wrap">{message.content}</p>
        </div>

        {message.toolCalls && message.toolCalls.length > 0 && (
          <div className="space-y-1.5 ml-1">
            {message.toolCalls.map((tc, i) => (
              <Collapsible key={i}>
                <CollapsibleTrigger className="flex items-center gap-2 text-xs text-muted-foreground hover:text-foreground transition-colors cursor-pointer">
                  <Badge
                    variant="outline"
                    className={`font-mono text-[10px] h-5 ${tc.isError ? 'border-destructive text-destructive' : ''}`}
                  >
                    {tc.toolName}
                  </Badge>
                  <span className="text-muted-foreground/50">expand</span>
                </CollapsibleTrigger>
                <CollapsibleContent>
                  <div className="mt-1.5 rounded-xl bg-muted p-3 text-xs font-mono space-y-2">
                    <div>
                      <span className="font-semibold text-muted-foreground text-[10px] uppercase tracking-wide">Args</span>
                      <pre className="mt-1 whitespace-pre-wrap text-foreground/80">
                        {JSON.stringify(tc.arguments, null, 2)}
                      </pre>
                    </div>
                    <Separator />
                    <div>
                      <span className="font-semibold text-muted-foreground text-[10px] uppercase tracking-wide">Result</span>
                      <pre className="mt-1 whitespace-pre-wrap text-foreground/80">
                        {JSON.stringify(tc.result, null, 2)}
                      </pre>
                    </div>
                  </div>
                </CollapsibleContent>
              </Collapsible>
            ))}
          </div>
        )}

        {message.usage && (
          <div className="flex items-center gap-1.5 ml-1">
            <Badge variant="secondary" className="text-[10px] h-4 font-mono">
              {message.usage.inputTokens ?? '?'} in
            </Badge>
            <Badge variant="secondary" className="text-[10px] h-4 font-mono">
              {message.usage.outputTokens ?? '?'} out
            </Badge>
            <Badge variant="secondary" className="text-[10px] h-4 font-mono">
              {message.usage.totalTokens ?? '?'} total
            </Badge>
          </div>
        )}
      </div>
    </div>
  );
}
