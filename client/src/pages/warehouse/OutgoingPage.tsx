import { ArrowUpFromLine, Eye, Plus } from 'lucide-react';
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
import { useDemoUnitStore } from '@/data/demoUnitStore';
import { t, type TranslationKey } from '@/i18n';
import { formatDateTime, formatNumber } from '@/lib/format';
import { getUnitOptionLabel } from '@/lib/unit';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { EntityStatus, InventoryMovement, StatusTone } from '@/types/domain';

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

interface OutgoingFormState {
  productId: string;
  quantity: string;
  destination: string;
  notes: string;
  requestId: string;
}

export function OutgoingStockPage() {
  const { selectedWarehouse, showToast } = useApp();
  const { session } = useDemoSession();
  const { products: allProducts, getProductSummary } = useDemoProductStore();
  const { units } = useDemoUnitStore();
  const { movements, recordMovement } = useDemoInventoryStore();
  const loader = useDemoLoader(movements);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [adding, setAdding] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [details, setDetails] = useState<InventoryMovement | null>(null);
  const [form, setForm] = useState<OutgoingFormState>({
    productId: '',
    quantity: '1',
    destination: '',
    notes: '',
    requestId: '',
  });
  const [productError, setProductError] = useState(false);
  const [quantityError, setQuantityError] = useState(false);
  const [destinationError, setDestinationError] = useState(false);

  const products = useMemo(
    () =>
      allProducts.filter(
        (product) =>
          product.warehouseId === selectedWarehouse.id && !product.isDeleted && product.isActive,
      ),
    [allProducts, selectedWarehouse.id],
  );

  const selectedProduct = products.find((product) => product.id === form.productId);
  const availableQuantity = selectedProduct ? getProductSummary(selectedProduct).onHandQuantity : 0;

  const productLabel = useCallback(
    (productId: string): string =>
      allProducts.find((product) => product.id === productId)?.name ?? productId,
    [allProducts],
  );

  function unitLabel(unitId: string, fallback = ''): string {
    const unit = units.find((item) => item.id === unitId);
    return unit ? getUnitOptionLabel(unit, t('warehouse.shared.archivedUnitSuffix')) : fallback;
  }

  const outgoing = useMemo(
    () =>
      movements.filter(
        (movement) =>
          movement.warehouseId === selectedWarehouse.id && movement.type === 'STOCK_OUT',
      ),
    [movements, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return outgoing.filter((movement) => {
      const matchesSearch =
        needle === '' ||
        `${productLabel(movement.productId)} ${movement.relatedTransaction} ${movement.notes}`
          .toLocaleLowerCase()
          .includes(needle);
      const matchesStatus = statusFilter === '' || movement.status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [outgoing, search, statusFilter, productLabel]);

  const totalQuantity = useMemo(
    () => outgoing.reduce((total, movement) => total + movement.quantity, 0),
    [outgoing],
  );

  const columns: DataTableColumn<InventoryMovement>[] = [
    {
      id: 'date',
      headerKey: 'warehouse.outgoing.dateColumn',
      cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
      sortValue: (row) => row.occurredAt,
      ltr: true,
    },
    {
      id: 'product',
      headerKey: 'warehouse.outgoing.productColumn',
      cell: (row) => <strong>{productLabel(row.productId)}</strong>,
      sortValue: (row) => productLabel(row.productId),
    },
    {
      id: 'quantity',
      headerKey: 'warehouse.outgoing.quantityColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.quantity)}</span>,
      sortValue: (row) => row.quantity,
      align: 'end',
      ltr: true,
    },
    {
      id: 'unit',
      headerKey: 'warehouse.outgoing.unitColumn',
      cell: (row) => unitLabel(row.unitId, row.unitId),
    },
    {
      id: 'destination',
      headerKey: 'warehouse.outgoing.destinationColumn',
      cell: (row) => row.notes.split(' — ')[0] || t('common.states.notAvailable'),
      sortValue: (row) => row.notes,
    },
    {
      id: 'transaction',
      headerKey: 'warehouse.outgoing.transactionColumn',
      cell: (row) => <span dir="ltr">{row.relatedTransaction}</span>,
      sortValue: (row) => row.relatedTransaction,
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'warehouse.outgoing.statusColumn',
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
        </div>
      ),
    },
  ];

  function openAdd() {
    setAdding(true);
    setForm({
      productId: products[0]?.id ?? '',
      quantity: '1',
      destination: '',
      notes: '',
      requestId: makeInventoryRequestId('issue'),
    });
    setProductError(false);
    setQuantityError(false);
    setDestinationError(false);
  }

  function closeModal() {
    setAdding(false);
    setConfirming(false);
    setProductError(false);
    setQuantityError(false);
    setDestinationError(false);
  }

  function saveForm() {
    const quantity = Number(form.quantity);
    const productValid = form.productId !== '';
    const quantityValid = form.quantity.trim() !== '' && Number.isFinite(quantity) && quantity > 0;
    const destinationValid = form.destination.trim().length >= 2;
    setProductError(!productValid);
    setQuantityError(!quantityValid);
    setDestinationError(!destinationValid);
    if (!productValid || !quantityValid || !destinationValid) {
      return;
    }
    setConfirming(true);
  }

  function confirmSave() {
    const quantity = Number(form.quantity);
    const trimmedNotes = form.notes.trim();
    const notes = `${form.destination.trim()}${trimmedNotes ? ` — ${trimmedNotes}` : ''}`;
    const result = recordMovement({
      requestId: form.requestId,
      warehouseId: selectedWarehouse.id,
      productId: form.productId,
      type: 'STOCK_OUT',
      quantity,
      onHandDelta: -quantity,
      unitId: selectedProduct?.baseUnitId ?? '',
      responsibleUserId: session?.user.id ?? '',
      relatedTransaction: `TXN-LOCAL-${Date.now().toString().slice(-4)}`,
      notes,
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
        titleKey="warehouse.outgoing.title"
        descriptionKey="warehouse.outgoing.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
            <button type="button" className="button button--primary button--sm" onClick={openAdd}>
              <Plus size={14} aria-hidden="true" />
              {t('warehouse.outgoing.newTransaction')}
            </button>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.outgoing.title"
          value={formatNumber(outgoing.length)}
          icon={ArrowUpFromLine}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.outgoing.quantityColumn"
          value={formatNumber(totalQuantity)}
          icon={ArrowUpFromLine}
          tone="healthy"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.outgoing.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
              aria-label={t('warehouse.outgoing.statusFilter')}
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
          captionKey="warehouse.outgoing.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={adding}
        onClose={closeModal}
        titleKey="warehouse.outgoing.newTitle"
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
            labelKey="warehouse.outgoing.productField"
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
          <FormField
            labelKey="warehouse.outgoing.quantityField"
            required
            errorKey={quantityError ? 'common.form.positiveNumber' : undefined}
          >
            <input
              type="number"
              min="1"
              dir="ltr"
              value={form.quantity}
              onChange={(event) => {
                setForm((current) => ({ ...current, quantity: event.target.value }));
                setQuantityError(false);
              }}
            />
          </FormField>
          <FormField labelKey="warehouse.outgoing.unitField">
            <input value={unitLabel(selectedProduct?.baseUnitId ?? '')} readOnly />
          </FormField>
          <FormField labelKey="warehouse.inventory.expectedQuantityField">
            <input value={formatNumber(availableQuantity)} dir="ltr" readOnly />
          </FormField>
          <FormField
            labelKey="warehouse.outgoing.destinationField"
            required
            errorKey={destinationError ? 'common.form.fieldRequired' : undefined}
          >
            <input
              value={form.destination}
              onChange={(event) => {
                setForm((current) => ({ ...current, destination: event.target.value }));
                setDestinationError(false);
              }}
            />
          </FormField>
          <FormField labelKey="warehouse.outgoing.notesField">
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
        titleKey="warehouse.outgoing.detailsTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={formGridStyle}>
            <div>
              <strong>{t('warehouse.outgoing.productColumn')}</strong>
              <p>{productLabel(details.productId)}</p>
            </div>
            <div>
              <strong>{t('warehouse.outgoing.quantityColumn')}</strong>
              <p dir="ltr">{formatNumber(details.quantity)}</p>
            </div>
            <div>
              <strong>{t('warehouse.outgoing.destinationColumn')}</strong>
              <p>{details.notes.split(' — ')[0] || t('common.states.notAvailable')}</p>
            </div>
            <div>
              <strong>{t('warehouse.outgoing.transactionColumn')}</strong>
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
              <strong>{t('warehouse.outgoing.notesColumn')}</strong>
              <p>{details.notes}</p>
            </div>
          </div>
        ) : null}
      </Modal>
    </>
  );
}

export default OutgoingStockPage;
