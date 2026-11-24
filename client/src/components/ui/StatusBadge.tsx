import { t, type TranslationKey } from '@/i18n';
import type { StatusTone } from '@/types/domain';

export interface StatusBadgeProps {
  tone: StatusTone;
  labelKey: TranslationKey;
  dot?: boolean;
}

export function StatusBadge({ tone, labelKey, dot = true }: StatusBadgeProps) {
  return (
    <span className={`status-badge status-badge--${tone}`}>
      {dot ? <span className="status-badge__dot" aria-hidden="true" /> : null}
      {t(labelKey)}
    </span>
  );
}
