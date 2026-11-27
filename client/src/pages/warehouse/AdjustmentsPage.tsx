import { ClipboardCheck, Plus, ShieldCheck, SlidersHorizontal } from 'lucide-react';
import { useCallback, useMemo, useState } from 'react';
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

const reasonCodes = ['COUNT', 'DAMAGE', 'CORRECTION', 'OTHER'] as const;

type AdjustmentReasonCode = (typeof reasonCodes)[number];

const reasonLabels: Record<AdjustmentReasonCode, TranslationKey> = {
  COUNT: 'warehouse.adjustments.reasonCount',
  DAMAGE: 'warehouse.adjustments.reasonDamage',
  CORRECTION: 'warehouse.adjustments.reasonCorrection',
  OTHER: 'warehouse.adjustments.reasonOther',
};

const reasonOptions: readonly AdjustmentReasonCode[] = reasonCodes;

function reasonFromNotes(notes: string): AdjustmentReasonCode {
  return reasonCodes.find((code) => notes.startsWith(`${code}:`)) ?? 'CORRECTION';
}

function notesFromMovement(notes: string): string {
  const code = reasonCodes.find((item) => notes.startsWith(`${item}:`));
  return code ? notes.slice(code.length + 1) : notes;
}

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
  reasonCode: AdjustmentReasonCode;
  quantity: string;
  notes: string;
  requestId: string;
}

export function InventoryAdjustmentsPage() {
  const { selectedWarehouse, showToast } = useApp();
  const { session } = useDemoSession();
  const { products: allProducts, getProductSummary } = useDemoProductStore();
  const { movements, recordMovement, confirmMovement } = useDemoInventoryStore();
  const loader = useDemoLoader(movements);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [adding, setAdding] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [reviewing, setReviewing] = useState<AdjustmentRecord | null>(null);
  const [form, setForm] = useState<AdjustmentFormState>({
    productId: '',
    reasonCode: 'CORRECTION',
    quantity: '1',
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

  const productLabel = useCallback(
    (productId: string): string =>
      allProducts.find((product) => product.id === productId)?.name ?? productId,
    [allProducts],
  );

  const rows = useMemo(
    () =>
      movements
        .filter(
          (movement) =>
            movement.warehouseId === selectedWarehouse.id &&
            (movement.type === 'ADJUSTMENT' || movement.type === 'COUNT'),
        )
        .map((movement) => {
          const reasonCode = reasonFromNotes(movement.notes);
          return {
            origin: movement.origin,
            id: movement.id,
            warehouseId: movement.warehouseId,
            productId: movement.productId,
            reasonKey:
              movement.type === 'COUNT' && reasonCode === 'CORRECTION'
                ? reasonLabels.COUNT
                : reasonLabels[reasonCode],
            quantity: movement.quantity,
            notes: notesFromMovement(movement.notes),
            occurredAt: movement.occurredAt,
            status: movement.status,
          };
        })
        .sort((left, right) => right.occurredAt.localeCompare(left.occurredAt)),
    [movements, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return rows.filter((adjustment) => {
      const matchesSearch =
        needle === '' ||
        `${productLabel(adjustment.productId)} ${t(adjustment.reasonKey)} ${adjustment.notes}`
          .toLocaleLowerCase()
          .includes(needle);
      const matchesStatus = statusFilter === '' || adjustment.status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [rows, search, statusFilter, productLabel]);

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
      cell: (row) => <strong>{productLabel(row.productId)}</strong>,
      sortValue: (row) => productLabel(row.productId),
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
      reasonCode: 'CORRECTION',
      quantity: '1',
      notes: '',
      requestId: makeInventoryRequestId('adjust'),
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
    const quantity = Number(form.quantity);
    const productValid = form.productId !== '';
    const quantityValid =
      form.quantity.trim() !== '' && Number.isInteger(quantity) && quantity !== 0;
    setProductError(!productValid);
    setQuantityError(!quantityValid);
    if (!productValid || !quantityValid) {
      return;
    }
    setConfirming(true);
  }

  function confirmSave() {
    const quantity = Number(form.quantity);
    const result = recordMovement({
      requestId: form.requestId,
      warehouseId: selectedWarehouse.id,
      productId: form.productId,
      type: 'ADJUSTMENT',
      quantity,
      onHandDelta: quantity,
      unitId: selectedProduct?.baseUnitId ?? '',
      responsibleUserId: session?.user.id ?? '',
      relatedTransaction: `ADJ-LOCAL-${Date.now().toString().slice(-4)}`,
      notes: `${form.reasonCode}:${form.notes.trim()}`,
      status: 'posted',
    });
    if (!result.ok) {
      showToast(inventoryErrorKeys[result.errorCode], 'critical');
      return;
    }
    showToast('warehouse.inventory.recordedToast', 'info');
    closeModal();
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
          <FormField labelKey="warehouse.inventory.expectedQuantityField">
            <input value={formatNumber(expectedQuantity)} dir="ltr" readOnly />
          </FormField>
          <FormField labelKey="warehouse.adjustments.reasonField" required>
            <select
              value={form.reasonCode}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  reasonCode: event.target.value as AdjustmentReasonCode,
                }))
              }
            >
              {reasonOptions.map((reason) => (
                <option key={reason} value={reason}>
                  {t(reasonLabels[reason])}
                </option>
              ))}
            </select>
          </FormField>
          <FormField
            labelKey="warehouse.inventory.differenceField"
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
        open={confirming}
        onClose={() => setConfirming(false)}
        onConfirm={confirmSave}
        titleKey="warehouse.inventory.confirmTitle"
        messageKey="warehouse.inventory.confirmMessage"
      />

      <ConfirmDialog
        open={reviewing !== null}
        onClose={() => setReviewing(null)}
        onConfirm={() => {
          if (reviewing) {
            confirmMovement(reviewing.id);
          }
          showToast('warehouse.shared.confirmToast', 'info');
          setReviewing(null);
        }}
        titleKey="warehouse.shared.confirmReviewTitle"
        messageKey="warehouse.shared.confirmReviewMessage"
      />
    </>
  );
}

export default InventoryAdjustmentsPage;
