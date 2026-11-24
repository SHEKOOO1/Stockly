import type { LucideIcon } from 'lucide-react';
import { t, type TranslationKey } from '@/i18n';
import type { StatusTone } from '@/types/domain';
import { Sparkline } from './charts/Sparkline';

export interface StatCardProps {
  labelKey: TranslationKey;
  value: string | number;
  hintKey?: TranslationKey;
  icon: LucideIcon;
  tone?: StatusTone;
  trend?: readonly number[];
}

export function StatCard({
  labelKey,
  value,
  hintKey,
  icon: Icon,
  tone = 'info',
  trend,
}: StatCardProps) {
  return (
    <article className={`stat-card stat-card--${tone}`}>
      <div className="stat-card__top">
        <span className="stat-card__label">{t(labelKey)}</span>
        <span className="stat-card__icon" aria-hidden="true">
          <Icon size={18} />
        </span>
      </div>
      <span className="stat-card__value" dir="ltr">
        {value}
      </span>
      {hintKey ? <span className="stat-card__hint">{t(hintKey)}</span> : null}
      {trend && trend.length > 0 ? (
        <div className="stat-card__trend">
          <Sparkline values={trend} tone={tone} />
        </div>
      ) : null}
    </article>
  );
}
