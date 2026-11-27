import { Eye, History, ShieldCheck } from 'lucide-react';
import { useCallback, useMemo, useState } from 'react';
import {
  ConfirmDialog,
  DataTable,
  DemoNotice,
  ErrorState,
  LoadingState,
  Modal,
  PageHeader,
  SearchFilterBar,
  StatCard,
  StatusBadge,
  type DataTableColumn,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoUsers } from '@/data/demoData';
import { useDemoInventoryStore } from '@/data/demoInventoryStore';
import { useDemoProductStore } from '@/data/demoProductStore';
import { t, type TranslationKey } from '@/i18n';
import { formatDateTime, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { EntityStatus, InventoryMovement, MovementType, StatusTone } from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const actionRowStyle = {
  display: 'flex',
  flexWrap: 'wrap',
  gap: '4px',
  justifyContent: 'flex-end',
} as const;

const filterGridStyle = {
  display: 'flex',
  flexWrap: 'wrap',
  gap: '8px',
  alignItems: 'center',
} as const;

const filterSelectStyle = { minWidth: 140 } as const;

const movementTypes: readonly MovementType[] = [
  'STOCK_IN',
  'STOCK_OUT',
  'TRANSFER',
  'ADJUSTMENT',
  'COUNT',
];

const movementLabels: Record<MovementType, TranslationKey> = {
  STOCK_IN: 'warehouse.movements.typeStockIn',
  STOCK_OUT: 'warehouse.movements.typeStockOut',
  TRANSFER: 'warehouse.movements.typeTransfer',
  ADJUSTMENT: 'warehouse.movements.typeAdjustment',
  COUNT: 'warehouse.movements.typeCount',
};

const movementTones: Record<MovementType, StatusTone> = {
  STOCK_IN: 'healthy',
  STOCK_OUT: 'info',
  TRANSFER: 'neutral',
  ADJUSTMENT: 'warning',
  COUNT: 'neutral',
};

const statusLabels: Record<EntityStatus, TranslationKey> = {
  active: 'common.status.active',
  inactive: 'common.status.inactive',
  draft: 'common.status.draft',
  pending: 'common.status.pending',
  posted: 'common.status.posted',
  confirmed: 'common.status.confirmed',
};

const statusTones: Record<EntityStatus, StatusTone> = {
  active: 'healthy',
  inactive: 'neutral',
  draft: 'neutral',
  pending: 'warning',
  posted: 'info',
  confirmed: 'healthy',
};

export function StockMovementsPage() {
  const { selectedWarehouse, showToast } = useApp();
  const { products } = useDemoProductStore();
  const { movements, confirmMovement } = useDemoInventoryStore();
  const loader = useDemoLoader(movements);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<MovementType | ''>('');
  const [responsibleFilter, setResponsibleFilter] = useState('');
  const [relatedFilter, setRelatedFilter] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [details, setDetails] = useState<InventoryMovement | null>(null);
  const [reviewing, setReviewing] = useState<InventoryMovement | null>(null);

  const productLabel = useCallback(
    (productId: string): string =>
      products.find((product) => product.id === productId)?.name ?? productId,
    [products],
  );

  const rows = useMemo(
    () =>
      movements
        .filter((movement) => movement.warehouseId === selectedWarehouse.id)
        .sort((left, right) => right.occurredAt.localeCompare(left.occurredAt)),
    [movements, selectedWarehouse.id],
  );

  const responsibleUsers = useMemo(() => {
    const ids = new Set(rows.map((movement) => movement.responsibleUserId));
    return demoUsers.filter((user) => ids.has(user.id));
  }, [rows]);

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    const related = relatedFilter.trim().toLocaleLowerCase();
    return rows.filter((movement) => {
      const productName = productLabel(movement.productId);
      const matchesSearch =
        needle === '' || `${productName} ${movement.notes}`.toLocaleLowerCase().includes(needle);
      const matchesType = typeFilter === '' || movement.type === typeFilter;
      const matchesResponsible =
        responsibleFilter === '' || movement.responsibleUserId === responsibleFilter;
      const matchesRelated =
        related === '' || movement.relatedTransaction.toLocaleLowerCase().includes(related);
      const day = movement.occurredAt.slice(0, 10);
      const matchesFrom = dateFrom === '' || day >= dateFrom;
      const matchesTo = dateTo === '' || day <= dateTo;
      return (
        matchesSearch &&
        matchesType &&
        matchesResponsible &&
        matchesRelated &&
        matchesFrom &&
        matchesTo
      );
    });
  }, [rows, search, typeFilter, responsibleFilter, relatedFilter, dateFrom, dateTo, productLabel]);

  const postedCount = useMemo(
    () =>
      rows.filter((movement) => movement.status === 'posted' || movement.status === 'confirmed')
        .length,
    [rows],
  );

  const columns: DataTableColumn<InventoryMovement>[] = [
    {
      id: 'type',
      headerKey: 'warehouse.movements.typeColumn',
      cell: (row) => (
        <StatusBadge tone={movementTones[row.type]} labelKey={movementLabels[row.type]} />
      ),
      sortValue: (row) => row.type,
    },
    {
      id: 'product',
      headerKey: 'warehouse.movements.productColumn',
      cell: (row) => <strong>{productLabel(row.productId)}</strong>,
      sortValue: (row) => productLabel(row.productId),
    },
    {
      id: 'quantity',
      headerKey: 'warehouse.movements.quantityColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.quantity)}</span>,
      sortValue: (row) => row.quantity,
      align: 'end',
      ltr: true,
    },
    {
      id: 'date',
      headerKey: 'warehouse.movements.dateColumn',
      cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
      sortValue: (row) => row.occurredAt,
      ltr: true,
    },
    {
      id: 'responsible',
      headerKey: 'warehouse.movements.responsibleColumn',
      cell: (row) =>
        demoUsers.find((user) => user.id === row.responsibleUserId)?.name ??
        t('common.states.notAvailable'),
      sortValue: (row) => row.responsibleUserId,
    },
    {
      id: 'related',
      headerKey: 'warehouse.movements.transactionColumn',
      cell: (row) => <span dir="ltr">{row.relatedTransaction}</span>,
      sortValue: (row) => row.relatedTransaction,
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'warehouse.movements.statusColumn',
      cell: (row) => (
        <StatusBadge tone={statusTones[row.status]} labelKey={statusLabels[row.status]} />
      ),
    },
    {
      id: 'actions',
      headerKey: 'common.table.actions',
      cell: (row) => (
        <div style={actionRowStyle}>
          <button
            type="button"
            className="button button--quiet button--xs"
            onClick={() => setDetails(row)}
            aria-label={t('warehouse.shared.detailsAction')}
          >
            <Eye size={14} aria-hidden="true" />
            {t('common.actions.view')}
          </button>
          <button
            type="button"
            className="button button--quiet button--xs"
            onClick={() => setReviewing(row)}
            aria-label={t('warehouse.shared.confirmReviewTitle')}
          >
            <ShieldCheck size={14} aria-hidden="true" />
            {t('common.actions.confirm')}
          </button>
        </div>
      ),
    },
  ];

  function resetFilters() {
    setSearch('');
    setTypeFilter('');
    setResponsibleFilter('');
    setRelatedFilter('');
    setDateFrom('');
    setDateTo('');
  }

  function confirmReview() {
    if (!reviewing) {
      return;
    }
    confirmMovement(reviewing.id);
    showToast('warehouse.shared.confirmToast', 'info');
    setReviewing(null);
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.movements.title"
        descriptionKey="warehouse.movements.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.movements.title"
          value={formatNumber(rows.length)}
          icon={History}
          tone="info"
        />
        <StatCard
          labelKey="common.status.posted"
          value={formatNumber(postedCount)}
          icon={History}
          tone="healthy"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.movements.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <div style={filterGridStyle}>
              <select
                value={typeFilter}
                onChange={(event) => setTypeFilter(event.target.value as MovementType | '')}
                aria-label={t('warehouse.movements.typeFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {movementTypes.map((type) => (
                  <option key={type} value={type}>
                    {t(movementLabels[type])}
                  </option>
                ))}
              </select>
              <select
                value={responsibleFilter}
                onChange={(event) => setResponsibleFilter(event.target.value)}
                aria-label={t('warehouse.movements.responsibleFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {responsibleUsers.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.name}
                  </option>
                ))}
              </select>
              <input
                type="search"
                value={relatedFilter}
                onChange={(event) => setRelatedFilter(event.target.value)}
                placeholder={t('warehouse.movements.relatedFilter')}
                dir="ltr"
              />
              <input
                type="date"
                value={dateFrom}
                onChange={(event) => setDateFrom(event.target.value)}
                aria-label={t('warehouse.movements.dateFrom')}
                dir="ltr"
              />
              <input
                type="date"
                value={dateTo}
                onChange={(event) => setDateTo(event.target.value)}
                aria-label={t('warehouse.movements.dateTo')}
                dir="ltr"
              />
            </div>
          }
        />
      </div>

      {loader.status === 'error' ? (
        <ErrorState onRetry={loader.reload} />
      ) : loader.status === 'loading' ? (
        <LoadingState />
      ) : (
        <DataTable
          rows={filteredRows}
          columns={columns}
          rowKey={(row) => row.id}
          pageSize={8}
          captionKey="warehouse.movements.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={details !== null}
        onClose={() => setDetails(null)}
        titleKey="warehouse.movements.detailsTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={statGridStyle}>
            <div>
              <strong>{t('warehouse.movements.typeColumn')}</strong>
              <p>
                <StatusBadge
                  tone={movementTones[details.type]}
                  labelKey={movementLabels[details.type]}
                />
              </p>
            </div>
            <div>
              <strong>{t('warehouse.movements.quantityColumn')}</strong>
              <p dir="ltr">{formatNumber(details.quantity)}</p>
            </div>
            <div>
              <strong>{t('warehouse.movements.transactionColumn')}</strong>
              <p dir="ltr">{details.relatedTransaction}</p>
            </div>
            <div>
              <strong>{t('common.labels.status')}</strong>
              <p>
                <StatusBadge
                  tone={statusTones[details.status]}
                  labelKey={statusLabels[details.status]}
                />
              </p>
            </div>
            <div>
              <strong>{t('warehouse.movements.notesColumn')}</strong>
              <p>{details.notes}</p>
            </div>
          </div>
        ) : null}
      </Modal>

      <ConfirmDialog
        open={reviewing !== null}
        onClose={() => setReviewing(null)}
        onConfirm={confirmReview}
        titleKey="warehouse.shared.confirmReviewTitle"
        messageKey="warehouse.shared.confirmReviewMessage"
      />
    </>
  );
}

export default StockMovementsPage;
