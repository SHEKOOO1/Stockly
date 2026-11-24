import { ClipboardCheck, Plus, ShieldCheck, SlidersHorizontal } from 'lucide-react';
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
import { demoDataOrigin, demoMovements, demoProducts } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatDate, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { DataOrigin, EntityStatus, StatusTone } from '@/types/domain';

const formGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
  gap: '16px',
} as const;

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const actionRowStyle = {
  display: 'flex',
  justifyContent: 'flex-end',
} as const;

const filterSelectStyle = { minWidth: 160 } as const;

const statusOptions: readonly EntityStatus[] = ['pending', 'posted', 'confirmed'];

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

const reasonOptions: readonly TranslationKey[] = [
  'warehouse.adjustments.reasonCount',
  'warehouse.adjustments.reasonDamage',
  'warehouse.adjustments.reasonCorrection',
  'warehouse.adjustments.reasonOther',
];

interface AdjustmentRecord {
  origin: DataOrigin;
  id: string;
  warehouseId: string;
  productId: string;
  reasonKey: TranslationKey;
  quantity: number;
  notes: string;
  occurredAt: string;
  status: EntityStatus;
}

interface AdjustmentFormState {
  productId: string;
  reasonKey: TranslationKey;
  quantity: string;
  notes: string;
}

function initialAdjustments(): AdjustmentRecord[] {
  return demoMovements
    .filter((movement) => movement.type === 'ADJUSTMENT' || movement.type === 'COUNT')
    .map((movement) => ({
      origin: demoDataOrigin,
      id: movement.id,
      warehouseId: movement.warehouseId,
      productId: movement.productId,
      reasonKey:
        movement.type === 'COUNT'
          ? 'warehouse.adjustments.reasonCount'
          : 'warehouse.adjustments.reasonCorrection',
      quantity: movement.quantity,
      notes: movement.notes,
      occurredAt: movement.occurredAt,
      status: movement.status,
    }));
}

export function InventoryAdjustmentsPage() {
  const { selectedWarehouse, showToast } = useApp();
  const loader = useDemoLoader(demoMovements);
  const [localAdjustments, setLocalAdjustments] =
    useState<readonly AdjustmentRecord[]>(initialAdjustments);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [adding, setAdding] = useState(false);
  const [reviewing, setReviewing] = useState<AdjustmentRecord | null>(null);
  const [form, setForm] = useState<AdjustmentFormState>({
    productId: '',
    reasonKey: 'warehouse.adjustments.reasonCorrection',
    quantity: '1',
    notes: '',
  });
  const [productError, setProductError] = useState(false);
  const [quantityError, setQuantityError] = useState(false);

  const products = useMemo(
    () => demoProducts.filter((product) => product.warehouseId === selectedWarehouse.id),
    [selectedWarehouse.id],
  );

  const rows = useMemo(
    () =>
      localAdjustments
        .filter((adjustment) => adjustment.warehouseId === selectedWarehouse.id)
        .sort((left, right) => right.occurredAt.localeCompare(left.occurredAt)),
    [localAdjustments, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return rows.filter((adjustment) => {
      const productName =
        demoProducts.find((product) => product.id === adjustment.productId)?.name ?? '';
      const matchesSearch =
        needle === '' ||
        `${productName} ${t(adjustment.reasonKey)} ${adjustment.notes}`
          .toLocaleLowerCase()
          .includes(needle);
      const matchesStatus = statusFilter === '' || adjustment.status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [rows, search, statusFilter]);

  const pendingCount = useMemo(
    () => rows.filter((adjustment) => adjustment.status === 'pending').length,
    [rows],
  );

  const columns: DataTableColumn<AdjustmentRecord>[] = [
    {
      id: 'date',
      headerKey: 'warehouse.adjustments.dateColumn',
      cell: (row) => <span dir="ltr">{formatDate(row.occurredAt)}</span>,
      sortValue: (row) => row.occurredAt,
      ltr: true,
    },
    {
      id: 'product',
      headerKey: 'warehouse.adjustments.productColumn',
      cell: (row) => (
        <strong>
          {demoProducts.find((product) => product.id === row.productId)?.name ?? row.productId}
        </strong>
      ),
      sortValue: (row) => row.productId,
    },
    {
      id: 'reason',
      headerKey: 'warehouse.adjustments.reasonColumn',
      cell: (row) => t(row.reasonKey),
      sortValue: (row) => t(row.reasonKey),
    },
    {
      id: 'quantity',
      headerKey: 'warehouse.adjustments.quantityColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.quantity)}</span>,
      sortValue: (row) => row.quantity,
      align: 'end',
      ltr: true,
    },
    {
      id: 'notes',
      headerKey: 'warehouse.adjustments.notesColumn',
      cell: (row) => row.notes,
      sortValue: (row) => row.notes,
    },
    {
      id: 'status',
      headerKey: 'warehouse.adjustments.statusColumn',
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
            onClick={() => setReviewing(row)}
            aria-label={t('warehouse.adjustments.reviewAction')}
          >
            <ShieldCheck size={14} aria-hidden="true" />
            {t('warehouse.adjustments.reviewAction')}
          </button>
        </div>
      ),
    },
  ];

  function openAdd() {
    setAdding(true);
    setForm({
      productId: products[0]?.id ?? '',
      reasonKey: 'warehouse.adjustments.reasonCorrection',
      quantity: '1',
      notes: '',
    });
    setProductError(false);
    setQuantityError(false);
  }

  function closeModal() {
    setAdding(false);
    setProductError(false);
    setQuantityError(false);
  }

  function saveForm() {
    const quantity = Number(form.quantity);
    const productValid = form.productId !== '';
    const quantityValid =
      form.quantity.trim() !== '' && Number.isInteger(quantity) && quantity !== 0;
    setProductError(!productValid);
    setQuantityError(!quantityValid);
    if (!productValid || !quantityValid) {
      return;
    }

    setLocalAdjustments((current) => [
      ...current,
      {
        origin: demoDataOrigin,
        id: `adj-local-${Date.now()}`,
        warehouseId: selectedWarehouse.id,
        productId: form.productId,
        reasonKey: form.reasonKey,
        quantity,
        notes: form.notes.trim(),
        occurredAt: new Date().toISOString(),
        status: 'pending',
      },
    ]);
    showToast('warehouse.shared.createToast', 'info');
    closeModal();
  }

  function confirmReview() {
    if (!reviewing) {
      return;
    }
    setLocalAdjustments((current) =>
      current.map((adjustment) =>
        adjustment.id === reviewing.id ? { ...adjustment, status: 'confirmed' } : adjustment,
      ),
    );
    showToast('warehouse.shared.confirmToast', 'info');
    setReviewing(null);
  }

  function resetFilters() {
    setSearch('');
    setStatusFilter('');
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.adjustments.title"
        descriptionKey="warehouse.adjustments.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
            <button type="button" className="button button--primary button--sm" onClick={openAdd}>
              <Plus size={14} aria-hidden="true" />
              {t('warehouse.adjustments.newAdjustment')}
            </button>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.adjustments.title"
          value={formatNumber(rows.length)}
          icon={SlidersHorizontal}
          tone="info"
        />
        <StatCard
          labelKey="common.status.pending"
          value={formatNumber(pendingCount)}
          icon={ClipboardCheck}
          tone="warning"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.adjustments.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
              aria-label={t('warehouse.adjustments.statusFilter')}
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
          captionKey="warehouse.adjustments.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={adding}
        onClose={closeModal}
        titleKey="warehouse.adjustments.newTitle"
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
        <div style={formGridStyle}>
          <FormField
            labelKey="common.labels.product"
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
          <FormField labelKey="warehouse.adjustments.reasonField" required>
            <select
              value={form.reasonKey}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  reasonKey: event.target.value as TranslationKey,
                }))
              }
            >
              {reasonOptions.map((reason) => (
                <option key={reason} value={reason}>
                  {t(reason)}
                </option>
              ))}
            </select>
          </FormField>
          <FormField
            labelKey="warehouse.adjustments.quantityField"
            required
            errorKey={quantityError ? 'common.form.invalidNumber' : undefined}
          >
            <input
              type="number"
              step="1"
              dir="ltr"
              value={form.quantity}
              onChange={(event) => {
                setForm((current) => ({ ...current, quantity: event.target.value }));
                setQuantityError(false);
              }}
            />
          </FormField>
          <FormField labelKey="warehouse.adjustments.notesField">
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
        open={reviewing !== null}
        onClose={() => setReviewing(null)}
        onConfirm={confirmReview}
        titleKey="warehouse.shared.confirmReviewTitle"
        messageKey="warehouse.shared.confirmReviewMessage"
      />
    </>
  );
}

export default InventoryAdjustmentsPage;
