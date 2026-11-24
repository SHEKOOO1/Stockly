import type { ReactNode } from 'react';
import { t, type TranslationKey } from '@/i18n';

export interface PageHeaderProps {
  titleKey: TranslationKey;
  descriptionKey?: TranslationKey;
  eyebrowKey?: TranslationKey;
  actions?: ReactNode;
}

export function PageHeader({ titleKey, descriptionKey, eyebrowKey, actions }: PageHeaderProps) {
  return (
    <header className="page-header">
      <div className="page-header__text">
        {eyebrowKey ? <span className="page-header__eyebrow">{t(eyebrowKey)}</span> : null}
        <h1 className="page-header__title">{t(titleKey)}</h1>
        {descriptionKey ? <p className="page-header__description">{t(descriptionKey)}</p> : null}
      </div>
      {actions ? <div className="page-header__actions">{actions}</div> : null}
    </header>
  );
}
