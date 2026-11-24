import { Wrench } from 'lucide-react';
import { DemoNotice } from './DemoNotice';
import { PageHeader } from './PageHeader';
import { t, type TranslationKey } from '@/i18n';

export interface PlaceholderPageProps {
  titleKey: TranslationKey;
  descriptionKey: TranslationKey;
  placeholderKey: TranslationKey;
}

export function PlaceholderPage({
  titleKey,
  descriptionKey,
  placeholderKey,
}: PlaceholderPageProps) {
  return (
    <div className="placeholder-page">
      <PageHeader titleKey={titleKey} descriptionKey={descriptionKey} />
      <DemoNotice />
      <section className="placeholder-page__frame">
        <div className="placeholder-page__badge">
          <Wrench size={18} aria-hidden="true" />
          {t('common.demo.previewOnly')}
        </div>
        <p className="placeholder-page__message">{t(placeholderKey)}</p>
        <div className="placeholder-page__skeleton" aria-hidden="true">
          <span className="placeholder-page__skeleton-line" />
          <span className="placeholder-page__skeleton-line" />
          <span className="placeholder-page__skeleton-line" />
          <span className="placeholder-page__skeleton-block" />
        </div>
      </section>
    </div>
  );
}
