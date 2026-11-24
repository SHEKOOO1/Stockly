import { RotateCcw, Search } from 'lucide-react';
import type { ReactNode } from 'react';
import { formatNumber } from '@/lib/format';
import { t, type TranslationKey } from '@/i18n';

export interface SearchFilterBarProps {
  value: string;
  onChange: (value: string) => void;
  placeholderKey?: TranslationKey;
  filters?: ReactNode;
  resultCount?: number;
  onReset?: () => void;
}

export function SearchFilterBar({
  value,
  onChange,
  placeholderKey = 'common.table.search',
  filters,
  resultCount,
  onReset,
}: SearchFilterBarProps) {
  return (
    <div className="search-filter-bar">
      <label className="search-filter-bar__input">
        <Search size={16} aria-hidden="true" />
        <span className="sr-only">{t('common.actions.search')}</span>
        <input
          type="search"
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder={t(placeholderKey)}
        />
      </label>
      {filters ? <div className="search-filter-bar__filters">{filters}</div> : null}
      {typeof resultCount === 'number' ? (
        <span className="search-filter-bar__count" dir="ltr">
          {formatNumber(resultCount)}
          <span className="search-filter-bar__count-label">{t('common.search.results')}</span>
        </span>
      ) : null}
      {onReset ? (
        <button type="button" className="button button--quiet button--sm" onClick={onReset}>
          <RotateCcw size={14} aria-hidden="true" />
          {t('common.actions.reset')}
        </button>
      ) : null}
    </div>
  );
}
