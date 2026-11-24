import { CircleAlert, CircleCheck, Info, TriangleAlert, X, type LucideIcon } from 'lucide-react';
import { useEffect } from 'react';
import type { ToastMessage } from '@/context/AppContext';
import { t } from '@/i18n';
import type { StatusTone } from '@/types/domain';

export interface ToastProps {
  toast: ToastMessage;
  onDismiss: (id: string) => void;
  durationMs?: number;
}

const toneIcons = {
  healthy: CircleCheck,
  warning: TriangleAlert,
  critical: CircleAlert,
  neutral: Info,
  info: Info,
} satisfies Record<StatusTone, LucideIcon>;

export function Toast({ toast, onDismiss, durationMs = 4200 }: ToastProps) {
  const Icon = toneIcons[toast.tone];

  useEffect(() => {
    const timer = setTimeout(() => onDismiss(toast.id), durationMs);
    return () => clearTimeout(timer);
  }, [toast.id, onDismiss, durationMs]);

  return (
    <div
      className={`toast toast--${toast.tone}`}
      role={toast.tone === 'critical' ? 'alert' : 'status'}
    >
      <span className="toast__icon" aria-hidden="true">
        <Icon size={17} />
      </span>
      <span className="toast__message">{t(toast.messageKey, toast.values)}</span>
      <button
        type="button"
        className="toast__close"
        onClick={() => onDismiss(toast.id)}
        aria-label={t('common.actions.close')}
      >
        <X size={15} aria-hidden="true" />
      </button>
    </div>
  );
}
