import { useSyncExternalStore } from 'react';
import { logStore } from '@/lib/log-store';

export function useLogs() {
  const entries = useSyncExternalStore(logStore.subscribe.bind(logStore), logStore.getEntries.bind(logStore));
  return { entries, clear: logStore.clear.bind(logStore) };
}
