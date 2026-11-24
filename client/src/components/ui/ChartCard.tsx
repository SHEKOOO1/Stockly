import type { ReactNode } from 'react';
import { t, type TranslationKey } from '@/i18n';

export interface ChartCardProps {
  titleKey: TranslationKey;
  subtitleKey?: TranslationKey;
  actions?: ReactNode;
  children: ReactNode;
}

export function ChartCard({ titleKey, subtitleKey, actions, children }: ChartCardProps) {
  return (
    <section className="chart-card">
      <div className="chart-card__header">
        <div>
          <h2 className="chart-card__title">{t(titleKey)}</h2>
          {subtitleKey ? <p className="chart-card__subtitle">{t(subtitleKey)}</p> : null}
        </div>
        {actions ? <div className="chart-card__actions">{actions}</div> : null}
      </div>
      <div className="chart-card__body">{children}</div>
    </section>
  );
}
