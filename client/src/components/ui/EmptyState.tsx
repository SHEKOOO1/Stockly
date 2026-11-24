import { Inbox } from 'lucide-react';
import { t, type TranslationKey } from '@/i18n';

export interface EmptyStateProps {
  titleKey?: TranslationKey;
  descriptionKey?: TranslationKey;
  actionLabelKey?: TranslationKey;
  onAction?: () => void;
}

export function EmptyState({
  titleKey = 'common.states.empty',
  descriptionKey = 'common.states.emptyDescription',
  actionLabelKey,
  onAction,
}: EmptyStateProps) {
  return (
    <div className="state-block">
      <span className="state-block__icon" aria-hidden="true">
        <Inbox size={22} />
      </span>
      <h3 className="state-block__title">{t(titleKey)}</h3>
      <p className="state-block__description">{t(descriptionKey)}</p>
      {onAction && actionLabelKey ? (
        <button type="button" className="button button--primary button--sm" onClick={onAction}>
          {t(actionLabelKey)}
        </button>
      ) : null}
    </div>
  );
}
