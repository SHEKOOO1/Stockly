import { ArrowUpDown, ChevronDown, ChevronUp } from 'lucide-react';
import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { locale, t, type TranslationKey } from '@/i18n';
import { Pagination } from './Pagination';

export interface DataTableColumn<T> {
  id: string;
  headerKey: TranslationKey;
  cell: (row: T) => ReactNode;
  sortValue?: (row: T) => string | number;
  align?: 'start' | 'center' | 'end';
  ltr?: boolean;
  width?: string;
}

export interface DataTableProps<T> {
  rows: readonly T[];
  columns: readonly DataTableColumn<T>[];
  rowKey: (row: T) => string;
  search?: string;
  searchAccessor?: (row: T) => string;
  pageSize?: number;
  loading?: boolean;
  captionKey?: TranslationKey;
  emptyMessageKey?: TranslationKey;
  onRowClick?: (row: T) => void;
  className?: string;
  stickyHeader?: boolean;
}

interface SortState {
  columnId: string | null;
  direction: 'asc' | 'desc';
}

export function DataTable<T>({
  rows,
  columns,
  rowKey,
  search = '',
  searchAccessor,
  pageSize,
  loading = false,
  captionKey = 'common.labels.details',
  emptyMessageKey = 'common.states.emptyDescription',
  onRowClick,
  className = '',
  stickyHeader = false,
}: DataTableProps<T>) {
  const [sort, setSort] = useState<SortState>({ columnId: null, direction: 'asc' });
  const [page, setPage] = useState(1);

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase(locale);
    if (!needle || !searchAccessor) {
      return [...rows];
    }
    return rows.filter((row) => searchAccessor(row).toLocaleLowerCase(locale).includes(needle));
  }, [rows, search, searchAccessor]);

  const sortedRows = useMemo(() => {
    if (!sort.columnId) {
      return filteredRows;
    }
    const column = columns.find((item) => item.id === sort.columnId);
    const sortValue = column?.sortValue;
    if (!sortValue) {
      return filteredRows;
    }
    const factor = sort.direction === 'asc' ? 1 : -1;
    return [...filteredRows].sort((left, right) => {
      const leftValue = sortValue(left);
      const rightValue = sortValue(right);
      if (typeof leftValue === 'number' && typeof rightValue === 'number') {
        return (leftValue - rightValue) * factor;
      }
      return String(leftValue).localeCompare(String(rightValue), locale) * factor;
    });
  }, [filteredRows, columns, sort]);

  useEffect(() => {
    setPage(1);
  }, [search, sort.columnId, sort.direction, rows]);

  const effectivePageSize = pageSize ?? Math.max(sortedRows.length, 1);
  const pageRows = sortedRows.slice((page - 1) * effectivePageSize, page * effectivePageSize);
  const showPagination = pageSize !== undefined && sortedRows.length > pageSize;

  function toggleSort(columnId: string) {
    setSort((current) => ({
      columnId,
      direction: current.columnId === columnId && current.direction === 'asc' ? 'desc' : 'asc',
    }));
  }

  return (
    <div className={`data-table ${stickyHeader ? 'data-table--sticky' : ''} ${className}`.trim()}>
      <div className="data-table__scroll">
        <table className="data-table__table">
          <caption className="sr-only">{t(captionKey)}</caption>
          <thead>
            <tr>
              {columns.map((column) => {
                const isSorted = sort.columnId === column.id;
                const sortLabelKey =
                  isSorted && sort.direction === 'asc'
                    ? 'common.table.sortDescending'
                    : 'common.table.sortAscending';
                return (
                  <th
                    key={column.id}
                    scope="col"
                    className={column.align ? `is-align-${column.align}` : undefined}
                    style={column.width ? { width: column.width } : undefined}
                    aria-sort={
                      isSorted ? (sort.direction === 'asc' ? 'ascending' : 'descending') : undefined
                    }
                  >
                    {column.sortValue ? (
                      <button
                        type="button"
                        className="data-table__sort"
                        onClick={() => toggleSort(column.id)}
                        aria-label={t(sortLabelKey)}
                      >
                        <span>{t(column.headerKey)}</span>
                        {isSorted ? (
                          sort.direction === 'asc' ? (
                            <ChevronUp size={13} aria-hidden="true" />
                          ) : (
                            <ChevronDown size={13} aria-hidden="true" />
                          )
                        ) : (
                          <ArrowUpDown size={13} aria-hidden="true" />
                        )}
                      </button>
                    ) : (
                      t(column.headerKey)
                    )}
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={columns.length}>
                  <div className="data-table__state">{t('common.states.loading')}</div>
                </td>
              </tr>
            ) : pageRows.length === 0 ? (
              <tr>
                <td colSpan={columns.length}>
                  <div className="data-table__state">{t(emptyMessageKey)}</div>
                </td>
              </tr>
            ) : (
              pageRows.map((row) => (
                <tr
                  key={rowKey(row)}
                  className={onRowClick ? 'is-clickable' : undefined}
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  tabIndex={onRowClick ? 0 : undefined}
                  onKeyDown={
                    onRowClick
                      ? (event) => {
                          if (event.key === 'Enter' || event.key === ' ') {
                            event.preventDefault();
                            onRowClick(row);
                          }
                        }
                      : undefined
                  }
                >
                  {columns.map((column) => (
                    <td
                      key={column.id}
                      data-label={t(column.headerKey)}
                      className={`${column.ltr ? 'is-ltr ' : ''}${
                        column.align ? `is-align-${column.align}` : ''
                      }`.trim()}
                    >
                      {column.cell(row)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      {showPagination ? (
        <Pagination
          page={page}
          pageSize={effectivePageSize}
          total={sortedRows.length}
          onChange={setPage}
        />
      ) : null}
    </div>
  );
}
