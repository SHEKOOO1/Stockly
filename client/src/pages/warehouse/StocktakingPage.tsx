import { ClipboardList, Eye, Plus, ShieldCheck } from 'lucide-react';
import { useMemo, useState } from 'react';
import {
  ConfirmDialog,
  DataTable,
  DemoNotice,
  ErrorState,
  FormField,
  LoadingState,
  Modal,
  PageHeader,
  SearchFilterBar,
  StatCard,
  StatusBadge,
  type DataTableColumn,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { useDemoSession } from '@/context/DemoSessionContext';
import {
  inventoryErrorKeys,
  makeInventoryRequestId,
  useDemoInventoryStore,
} from '@/data/demoInventoryStore';
import { useDemoProductStore } from '@/data/demoProductStore';
import { t, type TranslationKey } from '@/i18n';
import { formatDate, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { EntityStatus, StatusTone, StocktakingSession } from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const detailGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
  gap: '16px',
} as const;

const actionRowStyle = {
  display: 'flex',
  flexWrap: 'wrap',
  gap: '4px',
  justifyContent: 'flex-end',
} as const;

const filterSelectStyle = { minWidth: 160 } as const;

const statusOptions: readonly EntityStatus[] = ['draft', 'pending', 'posted', 'confirmed'];

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

interface CountFormState {
  productId: string;
  countedQuantity: string;
  notes: string;
  requestId: string;
}

export function StocktakingPage() {
  const { selectedWarehouse, showToast } = useApp();
  const { session } = useDemoSession();
  const { products: allProducts, getProductSummary } = useDemoProductStore();
  const { stocktakingSessions, recordStockCount, confirmStocktakingSession } =
    useDemoInventoryStore();
  const loader = useDemoLoader(stocktakingSessions);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [details, setDetails] = useState<StocktakingSession | null>(null);
  const [reviewing, setReviewing] = useState<StocktakingSession | null>(null);
  const [adding, setAdding] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [form, setForm] = useState<CountFormState>({
    productId: '',
    countedQuantity: '0',
    notes: '',
    requestId: '',
  });
  const [productError, setProductError] = useState(false);
  const [quantityError, setQuantityError] = useState(false);

  const products = useMemo(
    () =>
      allProducts.filter(
        (product) =>
          product.warehouseId === selectedWarehouse.id && !product.isDeleted && product.isActive,
      ),
    [allProducts, selectedWarehouse.id],
  );

  const selectedProduct = products.find((product) => product.id === form.productId);
  const expectedQuantity = selectedProduct ? getProductSummary(selectedProduct).onHandQuantity : 0;
  const countedQuantity = Number(form.countedQuantity);
  const difference = Number.isFinite(countedQuantity) ? countedQuantity - expectedQuantity : 0;

  const rows = useMemo(
    () =>
      stocktakingSessions
        .filter((stocktakingSession) => stocktakingSession.warehouseId === selectedWarehouse.id)
        .sort((left, right) => right.startedAt.localeCompare(left.startedAt)),
    [stocktakingSessions, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return rows.filter((stocktakingSession) => {
      const matchesSearch =
        needle === '' || stocktakingSession.code.toLocaleLowerCase().includes(needle);
      const matchesStatus = statusFilter === '' || stocktakingSession.status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [rows, search, statusFilter]);

  const totals = useMemo(
    () =>
      rows.reduce(
        (acc, stocktakingSession) => ({
          expected: acc.expected + stocktakingSession.expectedLines,
          counted: acc.counted + stocktakingSession.countedLines,
          differences: acc.differences + stocktakingSession.differenceLines,
        }),
        { expected: 0, counted: 0, differences: 0 },
      ),
    [rows],
  );

  const columns: DataTableColumn<StocktakingSession>[] = [
    {
      id: 'session',
      headerKey: 'warehouse.stocktaking.sessionColumn',
      cell: (row) => <strong dir="ltr">{row.code}</strong>,
      sortValue: (row) => row.code,
      ltr: true,
    },
    {
      id: 'date',
      headerKey: 'warehouse.stocktaking.dateColumn',
      cell: (row) => <span dir="ltr">{formatDate(row.startedAt)}</span>,
      sortValue: (row) => row.startedAt,
      ltr: true,
    },
    {
      id: 'expected',
      headerKey: 'warehouse.stocktaking.expectedColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.expectedLines)}</span>,
      sortValue: (row) => row.expectedLines,
      align: 'end',
      ltr: true,
    },
    {
      id: 'counted',
      headerKey: 'warehouse.stocktaking.countedColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.countedLines)}</span>,
      sortValue: (row) => row.countedLines,
      align: 'end',
      ltr: true,
    },
    {
      id: 'difference',
      headerKey: 'warehouse.stocktaking.differenceColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.differenceLines)}</span>,
      sortValue: (row) => row.differenceLines,
      align: 'end',
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'warehouse.stocktaking.statusColumn',
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
            aria-label={t('warehouse.stocktaking.reviewAction')}
          >
            <ShieldCheck size={14} aria-hidden="true" />
            {t('warehouse.stocktaking.reviewAction')}
          </button>
        </div>
      ),
    },
  ];

  function resetFilters() {
    setSearch('');
    setStatusFilter('');
  }

  function openAdd() {
    setAdding(true);
    setForm({
      productId: products[0]?.id ?? '',
      countedQuantity: String(products[0] ? getProductSummary(products[0]).onHandQuantity : 0),
      notes: '',
      requestId: makeInventoryRequestId('count'),
    });
    setProductError(false);
    setQuantityError(false);
  }

  function closeModal() {
    setAdding(false);
    setConfirming(false);
    setProductError(false);
    setQuantityError(false);
  }

  function saveForm() {
    const quantityValid =
      form.countedQuantity.trim() !== '' &&
      Number.isInteger(countedQuantity) &&
      countedQuantity >= 0;
    setProductError(form.productId === '');
    setQuantityError(!quantityValid);
    if (form.productId === '' || !quantityValid) {
      return;
    }
    setConfirming(true);
  }

  function confirmSave() {
    const result = recordStockCount({
      requestId: form.requestId,
      warehouseId: selectedWarehouse.id,
      productId: form.productId,
      countedQuantity,
      responsibleUserId: session?.user.id ?? '',
      notes: form.notes.trim(),
    });
    if (!result.ok) {
      showToast(inventoryErrorKeys[result.errorCode], 'critical');
      return;
    }
    showToast('warehouse.inventory.countRecordedToast', 'info');
    closeModal();
  }

  function confirmReview() {
    if (!reviewing) {
      return;
    }
    confirmStocktakingSession(reviewing.id);
    showToast('warehouse.shared.confirmToast', 'info');
    setReviewing(null);
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.stocktaking.title"
        descriptionKey="warehouse.stocktaking.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
            <button type="button" className="button button--primary button--sm" onClick={openAdd}>
              <Plus size={14} aria-hidden="true" />
              {t('warehouse.inventory.newCount')}
            </button>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.stocktaking.expectedLines"
          value={formatNumber(totals.expected)}
          icon={ClipboardList}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.stocktaking.countedLines"
          value={formatNumber(totals.counted)}
          icon={ClipboardList}
          tone="healthy"
        />
        <StatCard
          labelKey="warehouse.stocktaking.differenceLines"
          value={formatNumber(totals.differences)}
          icon={ClipboardList}
          tone="warning"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.stocktaking.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
              aria-label={t('warehouse.stocktaking.statusFilter')}
              style={filterSelectStyle}
            >
              <option value="">{t('common.labels.all')}</option>
              {statusOptions.map((status) => (
                <option key={status} value={status}>
                  {t(statusLabels[status])}
                </option>
              ))}
            </select>
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
          pageSize={7}
          captionKey="warehouse.stocktaking.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={adding}
        onClose={closeModal}
        titleKey="warehouse.inventory.newCountTitle"
        descriptionKey="warehouse.shared.formDescription"
        footer={
          <>
            <button type="button" className="button button--quiet" onClick={closeModal}>
              {t('common.actions.cancel')}
            </button>
            <button type="button" className="button button--primary" onClick={saveForm}>
              {t('warehouse.shared.savePreview')}
            </button>
          </>
        }
      >
        <div style={detailGridStyle}>
          <FormField
            labelKey="warehouse.inventory.countProductField"
            required
            errorKey={productError ? 'common.form.fieldRequired' : undefined}
          >
            <select
              value={form.productId}
              onChange={(event) => {
                setForm((current) => ({ ...current, productId: event.target.value }));
                setProductError(false);
              }}
            >
              {products.map((product) => (
                <option key={product.id} value={product.id}>
                  {product.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField labelKey="warehouse.inventory.expectedQuantityField">
            <input value={formatNumber(expectedQuantity)} dir="ltr" readOnly />
          </FormField>
          <FormField
            labelKey="warehouse.inventory.countedQuantityField"
            required
            errorKey={quantityError ? 'common.form.invalidNumber' : undefined}
          >
            <input
              type="number"
              min="0"
              step="1"
              dir="ltr"
              value={form.countedQuantity}
              onChange={(event) => {
                setForm((current) => ({ ...current, countedQuantity: event.target.value }));
                setQuantityError(false);
              }}
            />
          </FormField>
          <FormField labelKey="warehouse.inventory.differenceField">
            <input value={formatNumber(difference)} dir="ltr" readOnly />
          </FormField>
          <FormField labelKey="warehouse.stocktaking.notesField">
            <input
              value={form.notes}
              onChange={(event) =>
                setForm((current) => ({ ...current, notes: event.target.value }))
              }
            />
          </FormField>
        </div>
      </Modal>

      <ConfirmDialog
        open={confirming}
        onClose={() => setConfirming(false)}
        onConfirm={confirmSave}
        titleKey="warehouse.inventory.confirmTitle"
        messageKey="warehouse.inventory.confirmMessage"
      />

      <Modal
        open={details !== null}
        onClose={() => setDetails(null)}
        titleKey="warehouse.stocktaking.reviewTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={detailGridStyle}>
            <div>
              <strong>{t('warehouse.stocktaking.sessionColumn')}</strong>
              <p dir="ltr">{details.code}</p>
            </div>
            <div>
              <strong>{t('warehouse.stocktaking.expectedColumn')}</strong>
              <p dir="ltr">{formatNumber(details.expectedLines)}</p>
            </div>
            <div>
              <strong>{t('warehouse.stocktaking.countedColumn')}</strong>
              <p dir="ltr">{formatNumber(details.countedLines)}</p>
            </div>
            <div>
              <strong>{t('warehouse.stocktaking.differenceColumn')}</strong>
              <p dir="ltr">{formatNumber(details.differenceLines)}</p>
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

export default StocktakingPage;
