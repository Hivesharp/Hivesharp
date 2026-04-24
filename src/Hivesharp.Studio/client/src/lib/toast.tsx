import { toast as sonnerToast, type ExternalToast } from 'sonner';
import { CircleX, CircleCheck, CircleAlert, Info } from 'lucide-react';

interface ToastOptions extends ExternalToast {
  description?: string;
}

function truncate(text: string, max: number): string {
  return text.length > max ? text.slice(0, max) + '...' : text;
}

function toastContent(
  icon: React.ReactNode,
  borderColor: string,
  bgColor: string,
  title: string,
  description?: string,
) {
  const desc = description ? truncate(description, 200) : undefined;
  return (
    <div className={`flex items-start gap-3 rounded-lg border-l-[3px] ${borderColor} ${bgColor} px-4 py-3 text-sm shadow-lg`}>
      <span className="mt-0.5 shrink-0">{icon}</span>
      <div className="min-w-0">
        <p className="font-medium text-foreground">{title}</p>
        {desc && <p className="mt-1 text-xs text-muted-foreground">{desc}</p>}
      </div>
    </div>
  );
}

export const toast = {
  error(title: string, opts?: ToastOptions) {
    return sonnerToast.custom(
      () => toastContent(
        <CircleX className="h-4 w-4 text-destructive" />,
        'border-destructive',
        'bg-destructive/10',
        title,
        opts?.description,
      ),
      { duration: 5000, ...opts },
    );
  },

  success(title: string, opts?: ToastOptions) {
    return sonnerToast.custom(
      () => toastContent(
        <CircleCheck className="h-4 w-4 text-emerald-500" />,
        'border-emerald-500',
        'bg-emerald-500/10',
        title,
        opts?.description,
      ),
      { duration: 5000, ...opts },
    );
  },

  warning(title: string, opts?: ToastOptions) {
    return sonnerToast.custom(
      () => toastContent(
        <CircleAlert className="h-4 w-4 text-amber-500" />,
        'border-amber-500',
        'bg-amber-500/10',
        title,
        opts?.description,
      ),
      { duration: 5000, ...opts },
    );
  },

  info(title: string, opts?: ToastOptions) {
    return sonnerToast.custom(
      () => toastContent(
        <Info className="h-4 w-4 text-blue-500" />,
        'border-blue-500',
        'bg-blue-500/10',
        title,
        opts?.description,
      ),
      { duration: 5000, ...opts },
    );
  },
};
