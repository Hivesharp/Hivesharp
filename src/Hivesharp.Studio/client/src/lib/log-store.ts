export type LogLevel = 'info' | 'error' | 'warning' | 'debug';

export interface LogEntry {
  id: number;
  timestamp: Date;
  level: LogLevel;
  message: string;
  detail?: string;
  method?: string;
  url?: string;
  status?: number;
  duration?: number;
}

type Listener = () => void;

let nextId = 1;
let entries: LogEntry[] = [];
const listeners = new Set<Listener>();

const MAX_ENTRIES = 500;

function notify() {
  for (const fn of listeners) fn();
}

export const logStore = {
  getEntries(): readonly LogEntry[] {
    return entries;
  },

  subscribe(listener: Listener): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },

  add(level: LogLevel, message: string, extra?: Partial<LogEntry>) {
    entries = [{ id: nextId++, timestamp: new Date(), level, message, ...extra }, ...entries].slice(0, MAX_ENTRIES);
    notify();
  },

  clear() {
    entries = [];
    notify();
  },

  info(message: string, extra?: Partial<LogEntry>) {
    this.add('info', message, extra);
  },

  error(message: string, extra?: Partial<LogEntry>) {
    this.add('error', message, extra);
  },

  warning(message: string, extra?: Partial<LogEntry>) {
    this.add('warning', message, extra);
  },

  debug(message: string, extra?: Partial<LogEntry>) {
    this.add('debug', message, extra);
  },
};
