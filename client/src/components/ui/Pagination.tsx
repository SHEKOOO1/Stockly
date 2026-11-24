import { ChevronLeft, ChevronRight } from 'lucide-react';
import { formatNumber } from '@/lib/format';
import { t } from '@/i18n';

export interface PaginationProps {
  page: number;
  pageSize: number;
  total: number;
  onChange: (page: number) => void;
}

function pageWindow(current: number, pageCount: number): number[] {
  const start = Math.max(1, current - 2);
  const end = Math.min(pageCount, start + 4);
  return Array.from({ length: end - start + 1 }, (_, index) => start + index);
}

export function Pagination({ page, pageSize, total, onChange }: PaginationProps) {
  const pageCount = Math.max(1, Math.ceil(total / pageSize));
  const currentPage = Math.min(Math.max(page, 1), pageCount);
  const start = total === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const end = Math.min(total, currentPage * pageSize);

  return (
    <nav className="pagination" aria-label={t('common.labels.page')}>
      <span className="pagination__summary">
        <span dir="ltr">
          {formatNumber(start)}–{formatNumber(end)}
        </span>
        <span>{t('common.labels.of')}</span>
        <span dir="ltr">{formatNumber(total)}</span>
        <span>{t('common.labels.rows')}</span>
      </span>
      <div className="pagination__controls">
        <button
          type="button"
          className="pagination__button"
          onClick={() => onChange(currentPage - 1)}
          disabled={currentPage <= 1}
          aria-label={t('common.actions.previous')}
        >
          <ChevronRight size={16} aria-hidden="true" />
        </button>
        {pageWindow(currentPage, pageCount).map((value) => (
          <button
            key={value}
            type="button"
            className={`pagination__button ${value === currentPage ? 'is-active' : ''}`.trim()}
            onClick={() => onChange(value)}
            aria-current={value === currentPage ? 'page' : undefined}
          >
            <span dir="ltr">{formatNumber(value)}</span>
          </button>
        ))}
        <button
          type="button"
          className="pagination__button"
          onClick={() => onChange(currentPage + 1)}
          disabled={currentPage >= pageCount}
          aria-label={t('common.actions.next')}
        >
          <ChevronLeft size={16} aria-hidden="true" />
        </button>
      </div>
    </nav>
  );
}
