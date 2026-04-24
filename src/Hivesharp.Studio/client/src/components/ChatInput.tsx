import { useState, useRef, useCallback } from 'react';
import { ArrowUp } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';

interface ChatInputProps {
  onSend: (message: string) => void;
  disabled: boolean;
  agentName?: string;
}

export default function ChatInput({ onSend, disabled, agentName }: ChatInputProps) {
  const [value, setValue] = useState('');
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const handleSend = useCallback(() => {
    const trimmed = value.trim();
    if (!trimmed) return;
    setValue('');
    onSend(trimmed);
  }, [value, onSend]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="border-t border-border bg-background px-6 py-4">
      <div className="max-w-3xl mx-auto flex items-end gap-2">
        <Textarea
          ref={textareaRef}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={agentName ? `Message ${agentName}...` : 'Type a message...'}
          disabled={disabled}
          className="min-h-10 max-h-40 resize-none"
          rows={1}
        />
        <Button
          onClick={handleSend}
          disabled={disabled || !value.trim()}
          size="icon"
          className="shrink-0 rounded-lg"
        >
          <ArrowUp className="size-4" />
        </Button>
      </div>
    </div>
  );
}
