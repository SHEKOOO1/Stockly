import { t, type TranslationKey } from '@/i18n';
import { Modal } from './Modal';

export interface ConfirmDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  titleKey: TranslationKey;
  messageKey: TranslationKey;
  confirmLabelKey?: TranslationKey;
  tone?: 'primary' | 'danger';
}

export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  titleKey,
  messageKey,
  confirmLabelKey = 'common.actions.confirm',
  tone = 'primary',
}: ConfirmDialogProps) {
  return (
    <Modal
      open={open}
      onClose={onClose}
      titleKey={titleKey}
      descriptionKey="common.form.localPreview"
      size="sm"
      footer={
        <>
          <button type="button" className="button button--quiet" onClick={onClose}>
            {t('common.actions.cancel')}
          </button>
          <button
            type="button"
            className={`button button--${tone}`}
            onClick={() => {
              onConfirm();
              onClose();
            }}
          >
            {t(confirmLabelKey)}
          </button>
        </>
      }
    >
      <p className="confirm-dialog__message">{t(messageKey)}</p>
    </Modal>
  );
}
