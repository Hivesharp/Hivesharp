import { useEffect, useState, useCallback } from 'react';
import type { Agent, AgentRuntimeState, ChatMessage, Thread, ThreadMessage } from '@/types';
import { fetchAgents, fetchAgentStatus, generateMessage, createThread, fetchThreads, fetchThreadMessages, fetchWorkingMemory, retryMcp } from '@/api';
import { toast } from '@/lib/toast';
import { logStore } from '@/lib/log-store';

function mapThreadMessages(msgs: ThreadMessage[]): ChatMessage[] {
  return msgs.map((m) => ({
    role: m.role as 'user' | 'assistant',
    content: m.content,
  }));
}

export function useAgentChat() {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [agentStatuses, setAgentStatuses] = useState<Record<string, AgentRuntimeState>>({});
  const [selectedAgentId, setSelectedAgentId] = useState<string | null>(null);
  const [messagesByKey, setMessagesByKey] = useState<Record<string, ChatMessage[]>>({});
  const [loading, setLoading] = useState(false);

  const [threadsByAgent, setThreadsByAgent] = useState<Record<string, Thread[]>>({});
  const [selectedThreadId, setSelectedThreadId] = useState<string | null>(null);
  const [workingMemory, setWorkingMemory] = useState<string | null>(null);

  useEffect(() => {
    fetchAgents().then((fetched) => {
      setAgents(fetched);
      // Fetch runtime status for all agents that have MCP servers configured
      const withMcp = fetched.filter((a) => a.mcpServers.length > 0);
      Promise.all(
        withMcp.map((a) =>
          fetchAgentStatus(a.id)
            .then((status) => ({ id: a.id, status }))
            .catch(() => null)
        )
      ).then((results) => {
        const statusMap: Record<string, AgentRuntimeState> = {};
        for (const r of results) {
          if (r) statusMap[r.id] = r.status;
        }
        setAgentStatuses(statusMap);
      });
    }).catch(() => {
      toast.error('Failed to load agents');
    });
  }, []);

  const selectedAgent = agents.find((a) => a.id === selectedAgentId) ?? null;

  const messageKey = selectedAgent?.hasMemory
    ? (selectedThreadId ?? selectedAgentId)
    : selectedAgentId;
  const messages = messageKey ? (messagesByKey[messageKey] ?? []) : [];
  const threads = selectedAgentId ? (threadsByAgent[selectedAgentId] ?? []) : [];

  const loadWorkingMemory = useCallback(async (agentId: string, threadId: string) => {
    const data = await fetchWorkingMemory(agentId, threadId);
    setWorkingMemory(data.content);
  }, []);

  const loadThreads = useCallback(async (agentId: string) => {
    const fetched = await fetchThreads(agentId);
    setThreadsByAgent((prev) => ({ ...prev, [agentId]: fetched }));
    return fetched;
  }, []);

  const handleSelectAgent = useCallback(async (agentId: string) => {
    setSelectedAgentId(agentId);
    const agent = agents.find((a) => a.id === agentId);

    if (agent?.hasMemory) {
      const agentThreads = await loadThreads(agentId);
      if (agentThreads.length > 0) {
        setSelectedThreadId(agentThreads[0].id);
        const msgs = await fetchThreadMessages(agentId, agentThreads[0].id);
        setMessagesByKey((prev) => ({ ...prev, [agentThreads[0].id]: mapThreadMessages(msgs) }));
        await loadWorkingMemory(agentId, agentThreads[0].id);
      } else {
        setSelectedThreadId(null);
        setWorkingMemory(null);
      }
    } else {
      setSelectedThreadId(null);
      setWorkingMemory(null);
    }
  }, [agents, loadThreads, loadWorkingMemory]);

  const handleSelectThread = useCallback(async (threadId: string) => {
    if (!selectedAgentId) return;
    setSelectedThreadId(threadId);

    if (!messagesByKey[threadId]) {
      const msgs = await fetchThreadMessages(selectedAgentId, threadId);
      setMessagesByKey((prev) => ({ ...prev, [threadId]: mapThreadMessages(msgs) }));
    }
    await loadWorkingMemory(selectedAgentId, threadId);
  }, [selectedAgentId, messagesByKey, loadWorkingMemory]);

  const handleNewThread = useCallback(async () => {
    if (!selectedAgentId) return;
    const thread = await createThread(selectedAgentId);
    setThreadsByAgent((prev) => ({
      ...prev,
      [selectedAgentId]: [thread, ...(prev[selectedAgentId] ?? [])],
    }));
    setSelectedThreadId(thread.id);
    setMessagesByKey((prev) => ({ ...prev, [thread.id]: [] }));
  }, [selectedAgentId]);

  const handleRetryMcp = useCallback(async (agentId: string) => {
    const runtimeState = await retryMcp(agentId);
    setAgentStatuses((prev) => ({ ...prev, [agentId]: runtimeState }));
  }, []);

  const handleSend = async (text: string) => {
    if (!selectedAgentId) return;

    const pendingKey = selectedAgent?.hasMemory
      ? (selectedThreadId ?? selectedAgentId)
      : selectedAgentId;

    const userMsg: ChatMessage = { role: 'user', content: text };
    setMessagesByKey((prev) => ({
      ...prev,
      [pendingKey]: [...(prev[pendingKey] ?? []), userMsg],
    }));

    setLoading(true);
    try {
      const threadId = selectedAgent?.hasMemory ? selectedThreadId : null;
      const data = await generateMessage(selectedAgentId, text, threadId);

      if (data.runtimeState) {
        setAgentStatuses((prev) => ({ ...prev, [selectedAgentId]: data.runtimeState! }));
      }

      if (data.toolCalls?.length > 0) {
        for (const tc of data.toolCalls) {
          const level = tc.isError ? 'warning' : 'debug';
          logStore.add(level, `tool: ${tc.toolName}`, {
            detail: `args:\n${JSON.stringify(tc.arguments, null, 2)}\n\nresult:\n${JSON.stringify(tc.result, null, 2)}`,
          });
        }
      }

      const assistantMsg: ChatMessage = {
        role: 'assistant',
        content: data.completion || '',
        toolCalls: data.toolCalls,
        usage: data.usage,
      };

      if (data.threadId && !selectedThreadId && selectedAgent?.hasMemory) {
        const newThreadId = data.threadId;
        setSelectedThreadId(newThreadId);
        await loadThreads(selectedAgentId);
        setMessagesByKey((prev) => {
          const next = { ...prev, [newThreadId]: [...(prev[pendingKey] ?? []), assistantMsg] };
          if (pendingKey !== newThreadId) {
            delete next[pendingKey];
          }
          return next;
        });
        await loadWorkingMemory(selectedAgentId, newThreadId);
        return;
      }

      setMessagesByKey((prev) => ({
        ...prev,
        [pendingKey]: [...(prev[pendingKey] ?? []), assistantMsg],
      }));

      if (selectedAgent?.hasMemory && selectedThreadId) {
        await loadWorkingMemory(selectedAgentId, selectedThreadId);
      }
    } catch (e) {
      toast.error('Failed to generate response');
    } finally {
      setLoading(false);
    }
  };

  return {
    agents,
    agentStatuses,
    selectedAgent,
    selectedAgentId,
    messages,
    loading,
    threads,
    selectedThreadId,
    workingMemory,
    handleSelectAgent,
    handleSelectThread,
    handleNewThread,
    handleSend,
    handleRetryMcp,
  };
}
