import { useApp } from '@/context/AppContext';
import { Toast } from './Toast';

export function ToastRegion() {
  const { toasts, dismissToast } = useApp();

  return (
    <div className="toast-region" aria-live="polite">
      {toasts.map((toast) => (
        <Toast key={toast.id} toast={toast} onDismiss={dismissToast} />
      ))}
    </div>
  );
}
