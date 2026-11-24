import { LoaderCircle } from 'lucide-react';
import { t, type TranslationKey } from '@/i18n';

export interface LoadingStateProps {
  titleKey?: TranslationKey;
  descriptionKey?: TranslationKey;
}

export function LoadingState({
  titleKey = 'common.states.loading',
  descriptionKey = 'common.states.loadingDescription',
}: LoadingStateProps) {
  return (
    <div className="state-block" role="status">
      <span className="state-block__icon state-block__icon--loading" aria-hidden="true">
        <LoaderCircle size={22} />
      </span>
      <h3 className="state-block__title">{t(titleKey)}</h3>
      <p className="state-block__description">{t(descriptionKey)}</p>
      <div className="state-block__skeleton" aria-hidden="true">
        <span />
        <span />
        <span />
      </div>
    </div>
  );
}
