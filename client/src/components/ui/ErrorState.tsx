import { CircleAlert, RotateCcw } from 'lucide-react';
import { t, type TranslationKey } from '@/i18n';

export interface ErrorStateProps {
  titleKey?: TranslationKey;
  descriptionKey?: TranslationKey;
  onRetry?: () => void;
}

export function ErrorState({
  titleKey = 'common.states.error',
  descriptionKey = 'common.states.errorDescription',
  onRetry,
}: ErrorStateProps) {
  return (
    <div className="state-block state-block--error" role="alert">
      <span className="state-block__icon" aria-hidden="true">
        <CircleAlert size={22} />
      </span>
      <h3 className="state-block__title">{t(titleKey)}</h3>
      <p className="state-block__description">{t(descriptionKey)}</p>
      {onRetry ? (
        <button type="button" className="button button--quiet button--sm" onClick={onRetry}>
          <RotateCcw size={14} aria-hidden="true" />
          {t('common.actions.retry')}
        </button>
      ) : null}
    </div>
  );
}
