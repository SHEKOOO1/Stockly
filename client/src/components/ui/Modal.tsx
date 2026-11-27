import { X } from 'lucide-react';
import { useEffect, useId, useRef, type ReactNode } from 'react';
import { t, type TranslationKey, type TranslationValues } from '@/i18n';

export interface ModalProps {
  open: boolean;
  onClose: () => void;
  titleKey: TranslationKey;
  titleValues?: TranslationValues;
  descriptionKey?: TranslationKey;
  footer?: ReactNode;
  size?: 'sm' | 'md' | 'lg';
  children: ReactNode;
}

export function Modal({
  open,
  onClose,
  titleKey,
  titleValues,
  descriptionKey,
  footer,
  size = 'md',
  children,
}: ModalProps) {
  const titleId = useId();
  const descriptionId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) {
      return;
    }
    dialogRef.current?.focus();
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onClose();
      }
    }
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  return (
    <div
      className="modal__backdrop"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <div
        ref={dialogRef}
        className={`modal modal--${size}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionKey ? descriptionId : undefined}
        tabIndex={-1}
      >
        <div className="modal__header">
          <div>
            <h2 id={titleId} className="modal__title">
              {t(titleKey, titleValues)}
            </h2>
            {descriptionKey ? (
              <p id={descriptionId} className="modal__description">
                {t(descriptionKey)}
              </p>
            ) : null}
          </div>
          <button
            type="button"
            className="icon-button"
            onClick={onClose}
            aria-label={t('common.actions.close')}
          >
            <X size={18} aria-hidden="true" />
          </button>
        </div>
        <div className="modal__body">{children}</div>
        {footer ? <div className="modal__footer">{footer}</div> : null}
      </div>
    </div>
  );
}
