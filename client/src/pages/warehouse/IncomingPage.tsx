import { ArrowDownToLine, Eye, Plus } from 'lucide-react';
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
import { useDemoSupplierStore } from '@/data/demoSupplierStore';
import { useDemoUnitStore } from '@/data/demoUnitStore';
import { t, type TranslationKey } from '@/i18n';
import { formatDateTime, formatNumber } from '@/lib/format';
import { getSupplierDisplayName, getSupplierOptionLabel } from '@/lib/supplier';
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

interface IncomingFormState {
  productId: string;
  quantity: string;
  supplierId: string;
  notes: string;
  requestId: string;
}

export function IncomingStockPage() {
  const { selectedOrganization, selectedWarehouse, showToast } = useApp();
  const { session } = useDemoSession();
  const { products: allProducts } = useDemoProductStore();
  const { units } = useDemoUnitStore();
  const { suppliers: allSuppliers, getSelectableSuppliers } = useDemoSupplierStore();
  const { movements, recordMovement } = useDemoInventoryStore();
  const loader = useDemoLoader(movements);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [adding, setAdding] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [details, setDetails] = useState<InventoryMovement | null>(null);
  const [form, setForm] = useState<IncomingFormState>({
    productId: '',
    quantity: '1',
    supplierId: '',
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

  const productLabel = useCallback(
    (productId: string): string =>
      allProducts.find((product) => product.id === productId)?.name ?? productId,
    [allProducts],
  );

  function unitLabel(unitId: string, fallback = ''): string {
    const unit = units.find((item) => item.id === unitId);
    return unit ? getUnitOptionLabel(unit, t('warehouse.shared.archivedUnitSuffix')) : fallback;
  }

  const selectableSuppliers = useMemo(
    () => getSelectableSuppliers(selectedOrganization.id, [form.supplierId]),
    [getSelectableSuppliers, selectedOrganization.id, form.supplierId],
  );

  function supplierFromNotes(notes: string) {
    return allSuppliers.find(
      (supplier) =>
        notes.startsWith(supplier.code) ||
        notes.startsWith(supplier.name) ||
        notes.includes(`(${supplier.code})`),
    );
  }

  const incoming = useMemo(
    () =>
      movements.filter(
        (movement) => movement.warehouseId === selectedWarehouse.id && movement.type === 'STOCK_IN',
      ),
    [movements, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return incoming.filter((movement) => {
      const matchesSearch =
        needle === '' ||
        `${productLabel(movement.productId)} ${movement.relatedTransaction} ${movement.notes}`
          .toLocaleLowerCase()
          .includes(needle);
      const matchesStatus = statusFilter === '' || movement.status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [incoming, search, statusFilter, productLabel]);

  const totalQuantity = useMemo(
    () => incoming.reduce((total, movement) => total + movement.quantity, 0),
    [incoming],
  );

  const columns: DataTableColumn<InventoryMovement>[] = [
    {
      id: 'date',
      headerKey: 'warehouse.incoming.dateColumn',
      cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
      sortValue: (row) => row.occurredAt,
      ltr: true,
    },
    {
      id: 'product',
      headerKey: 'warehouse.incoming.productColumn',
      cell: (row) => <strong>{productLabel(row.productId)}</strong>,
      sortValue: (row) => productLabel(row.productId),
    },
    {
      id: 'quantity',
      headerKey: 'warehouse.incoming.quantityColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.quantity)}</span>,
      sortValue: (row) => row.quantity,
      align: 'end',
      ltr: true,
    },
    {
      id: 'unit',
      headerKey: 'warehouse.incoming.unitColumn',
      cell: (row) => unitLabel(row.unitId, row.unitId),
    },
    {
      id: 'supplier',
      headerKey: 'warehouse.incoming.supplierColumn',
      cell: (row) =>
        getSupplierDisplayName(
          supplierFromNotes(row.notes),
          t('common.states.notAvailable'),
          t('warehouse.suppliers.archivedSuffix'),
        ),
      sortValue: (row) => supplierFromNotes(row.notes)?.name ?? row.notes,
    },
    {
      id: 'transaction',
      headerKey: 'warehouse.incoming.transactionColumn',
      cell: (row) => <span dir="ltr">{row.relatedTransaction}</span>,
      ltr: true,
      sortValue: (row) => row.relatedTransaction,
    },
    {
      id: 'status',
      headerKey: 'warehouse.incoming.statusColumn',
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
      supplierId: selectableSuppliers[0]?.id ?? '',
      notes: '',
      requestId: makeInventoryRequestId('receive'),
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
    const quantityValid = form.quantity.trim() !== '' && Number.isFinite(quantity) && quantity > 0;
    setProductError(!productValid);
    setQuantityError(!quantityValid);
    if (!productValid || !quantityValid) {
      return;
    }
    setConfirming(true);
  }

  function confirmSave() {
    const quantity = Number(form.quantity);
    const product = products.find((item) => item.id === form.productId);
    const supplier = allSuppliers.find((item) => item.id === form.supplierId);
    const trimmedNotes = form.notes.trim();
    const notes = supplier
      ? `${supplier.name} (${supplier.code})${trimmedNotes ? ` — ${trimmedNotes}` : ''}`
      : trimmedNotes;
    const result = recordMovement({
      requestId: form.requestId,
      warehouseId: selectedWarehouse.id,
      productId: form.productId,
      type: 'STOCK_IN',
      quantity,
      onHandDelta: quantity,
      unitId: product?.baseUnitId ?? '',
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
        titleKey="warehouse.incoming.title"
        descriptionKey="warehouse.incoming.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
            <button type="button" className="button button--primary button--sm" onClick={openAdd}>
              <Plus size={14} aria-hidden="true" />
              {t('warehouse.incoming.newTransaction')}
            </button>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.incoming.title"
          value={formatNumber(incoming.length)}
          icon={ArrowDownToLine}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.incoming.quantityColumn"
          value={formatNumber(totalQuantity)}
          icon={ArrowDownToLine}
          tone="healthy"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.incoming.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
              aria-label={t('warehouse.incoming.statusFilter')}
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
          captionKey="warehouse.incoming.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={adding}
        onClose={closeModal}
        titleKey="warehouse.incoming.newTitle"
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
            labelKey="warehouse.incoming.productField"
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
            labelKey="warehouse.incoming.quantityField"
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
          <FormField labelKey="warehouse.incoming.unitField">
            <input
              value={unitLabel(
                products.find((product) => product.id === form.productId)?.baseUnitId ?? '',
              )}
              readOnly
            />
          </FormField>
          <FormField labelKey="warehouse.incoming.supplierField">
            <select
              value={form.supplierId}
              onChange={(event) =>
                setForm((current) => ({ ...current, supplierId: event.target.value }))
              }
            >
              {selectableSuppliers.map((supplier) => (
                <option key={supplier.id} value={supplier.id}>
                  {getSupplierOptionLabel(supplier, t('warehouse.suppliers.archivedSuffix'))}
                </option>
              ))}
            </select>
          </FormField>
          <FormField labelKey="warehouse.incoming.notesField">
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
        titleKey="warehouse.incoming.detailsTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={formGridStyle}>
            <div>
              <strong>{t('warehouse.incoming.productColumn')}</strong>
              <p>{productLabel(details.productId)}</p>
            </div>
            <div>
              <strong>{t('warehouse.incoming.quantityColumn')}</strong>
              <p dir="ltr">{formatNumber(details.quantity)}</p>
            </div>
            <div>
              <strong>{t('warehouse.incoming.transactionColumn')}</strong>
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
              <strong>{t('warehouse.incoming.notesColumn')}</strong>
              <p>{details.notes}</p>
            </div>
          </div>
        ) : null}
      </Modal>
    </>
  );
}

export default IncomingStockPage;
