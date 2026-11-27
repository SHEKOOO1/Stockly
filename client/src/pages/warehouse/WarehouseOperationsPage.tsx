import { AlertTriangle, ArrowUpFromLine, Package, Plus } from 'lucide-react';
import { useCallback, useId, useMemo, useState } from 'react';
import {
  DataTable,
  DemoNotice,
  FormField,
  Modal,
  PageHeader,
  SearchFilterBar,
  StatCard,
  StatusBadge,
  type DataTableColumn,
} from '@/components/ui';
import { ProductFormModal } from '@/components/products/ProductFormModal';
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
import { formatNumber } from '@/lib/format';
import type { ProductDataset } from '@/lib/product';
import { getUnitOptionLabel } from '@/lib/unit';
import type { Product, ProductStockStatus, StatusTone } from '@/types/domain';

type StatusFilter = 'all' | ProductStockStatus;
type QuickMode = 'add' | 'issue';

interface OperationsRow {
  product: Product;
  quantity: number;
  minimum: number;
  unitLabel: string;
  status: ProductStockStatus;
}

interface QuickState {
  mode: QuickMode;
  product: Product;
  requestId: string;
}

const filterStyle = { minWidth: 180 } as const;
const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;
const actionsStyle = {
  display: 'flex',
  flexWrap: 'wrap',
  gap: '6px',
  justifyContent: 'flex-end',
} as const;

const statusKeys: Record<ProductStockStatus, TranslationKey> = {
  healthy: 'warehouse.operations.statusHealthy',
  low: 'warehouse.operations.statusLow',
  out: 'warehouse.operations.statusOut',
  expiring: 'warehouse.operations.statusExpiring',
};

const statusTones: Record<ProductStockStatus, StatusTone> = {
  healthy: 'healthy',
  low: 'warning',
  out: 'critical',
  expiring: 'info',
};

const statusOptions: readonly StatusFilter[] = ['all', 'low', 'out', 'expiring', 'healthy'];

function productDisplayName(product: Product): string {
  return product.nameAr || product.name;
}

function ProductAvatar({ product }: { product: Product }) {
  return (
    <span className="operations-avatar" aria-hidden="true">
      <Package size={18} />
      <span className="operations-avatar__initial">{product.name.trim().charAt(0)}</span>
    </span>
  );
}

export function WarehouseOperationsPage() {
  const { selectedWarehouse, showToast } = useApp();
  const { session } = useDemoSession();
  const { products, barcodes, settings, balances, batches, productSummaries, addProduct } =
    useDemoProductStore();
  const { units } = useDemoUnitStore();
  const { recordMovement } = useDemoInventoryStore();

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [addItemOpen, setAddItemOpen] = useState(false);
  const [quick, setQuick] = useState<QuickState | null>(null);
  const [quantity, setQuantity] = useState('1');
  const [reason, setReason] = useState('');
  const [quickError, setQuickError] = useState<TranslationKey | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const quantityFieldId = useId();

  const dataset = useMemo<ProductDataset>(
    () => ({ products, barcodes, settings, balances, batches }),
    [products, barcodes, settings, balances, batches],
  );

  const unitLabel = useCallback(
    (unitId: string) => {
      const unit = units.find((item) => item.id === unitId);
      return unit ? getUnitOptionLabel(unit, t('warehouse.shared.archivedUnitSuffix')) : unitId;
    },
    [units],
  );

  const rows = useMemo<OperationsRow[]>(
    () =>
      productSummaries
        .filter(
          (summary) =>
            summary.product.warehouseId === selectedWarehouse.id &&
            !summary.product.isDeleted &&
            summary.product.isActive,
        )
        .map((summary) => {
          const status: ProductStockStatus =
            summary.onHandQuantity <= 0
              ? 'out'
              : summary.minimumQuantity > 0 && summary.onHandQuantity <= summary.minimumQuantity
                ? 'low'
                : summary.stockStatus === 'expiring'
                  ? 'expiring'
                  : 'healthy';
          return {
            product: summary.product,
            quantity: summary.onHandQuantity,
            minimum: summary.minimumQuantity,
            unitLabel: unitLabel(summary.product.baseUnitId),
            status,
          };
        })
        .sort((left, right) => left.product.name.localeCompare(right.product.name, 'ar')),
    [productSummaries, selectedWarehouse.id, unitLabel],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase('ar');
    return rows.filter((row) => {
      const matchesSearch =
        needle === '' ||
        `${row.product.name} ${row.product.nameAr ?? ''} ${row.product.sku}`
          .toLocaleLowerCase('ar')
          .includes(needle);
      const matchesStatus = statusFilter === 'all' || row.status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [rows, search, statusFilter]);

  const lowCount = rows.filter((row) => row.status === 'low').length;
  const outCount = rows.filter((row) => row.status === 'out').length;

  const columns: DataTableColumn<OperationsRow>[] = [
    {
      id: 'product',
      headerKey: 'warehouse.products.nameColumn',
      cell: (row) => (
        <span className="operations-product">
          <ProductAvatar product={row.product} />
          <span>
            <strong>{row.product.nameAr || row.product.name}</strong>
            <small dir="ltr">{row.product.sku}</small>
          </span>
        </span>
      ),
      sortValue: (row) => row.product.name,
    },
    {
      id: 'quantity',
      headerKey: 'warehouse.operations.quantityColumn',
      cell: (row) => <strong dir="ltr">{formatNumber(row.quantity)}</strong>,
      sortValue: (row) => row.quantity,
      align: 'end',
      ltr: true,
    },
    {
      id: 'unit',
      headerKey: 'warehouse.operations.unitColumn',
      cell: (row) => row.unitLabel,
    },
    {
      id: 'minimum',
      headerKey: 'warehouse.operations.thresholdColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.minimum)}</span>,
      sortValue: (row) => row.minimum,
      align: 'end',
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'warehouse.operations.statusColumn',
      cell: (row) => (
        <StatusBadge tone={statusTones[row.status]} labelKey={statusKeys[row.status]} />
      ),
    },
    {
      id: 'actions',
      headerKey: 'warehouse.operations.actionsColumn',
      cell: (row) => (
        <div style={actionsStyle}>
          <button
            type="button"
            className="button button--primary button--xs"
            onClick={() => openQuick('add', row.product)}
          >
            <Plus size={14} aria-hidden="true" />
            {t('warehouse.operations.addQuantity')}
          </button>
          <button
            type="button"
            className="button button--quiet button--xs"
            onClick={() => openQuick('issue', row.product)}
          >
            <ArrowUpFromLine size={14} aria-hidden="true" />
            {t('warehouse.operations.issueQuantity')}
          </button>
        </div>
      ),
    },
  ];

  function openQuick(mode: QuickMode, product: Product) {
    setQuick({ mode, product, requestId: makeInventoryRequestId(mode) });
    setQuantity('1');
    setReason('');
    setQuickError(null);
    setSubmitting(false);
  }

  function closeQuick() {
    if (submitting) {
      return;
    }
    setQuick(null);
    setQuickError(null);
    setSubmitting(false);
  }

  function changeQuantity(next: number) {
    setQuantity((current) => String(Math.max(1, Number(current) + next)));
    setQuickError(null);
  }

  function submitQuick() {
    if (!quick || submitting) {
      return;
    }
    const value = Number(quantity);
    if (!Number.isInteger(value) || value <= 0) {
      setQuickError('warehouse.operations.invalidQuantity');
      return;
    }
    const currentRow = rows.find((row) => row.product.id === quick.product.id);
    if (quick.mode === 'issue' && value > (currentRow?.quantity ?? 0)) {
      setQuickError('warehouse.operations.insufficientStock');
      return;
    }
    setSubmitting(true);
    const result = recordMovement({
      requestId: quick.requestId,
      warehouseId: selectedWarehouse.id,
      productId: quick.product.id,
      type: quick.mode === 'add' ? 'STOCK_IN' : 'STOCK_OUT',
      quantity: value,
      onHandDelta: quick.mode === 'add' ? value : -value,
      unitId: quick.product.baseUnitId,
      responsibleUserId: session?.user.id ?? '',
      relatedTransaction: `OPS-${Date.now().toString().slice(-5)}`,
      notes: reason.trim(),
      status: 'posted',
    });
    if (!result.ok) {
      setQuickError(inventoryErrorKeys[result.errorCode]);
      setSubmitting(false);
      return;
    }
    showToast('warehouse.operations.recordedToast', 'info');
    setQuick(null);
    setQuickError(null);
    setSubmitting(false);
  }

  function saveProduct(values: Parameters<typeof addProduct>[1]) {
    addProduct(selectedWarehouse.id, values);
    setAddItemOpen(false);
    showToast('warehouse.shared.createToast', 'info');
  }

  const quickRow = quick ? rows.find((row) => row.product.id === quick.product.id) : undefined;
  const parsedQuantity = Number(quantity);
  const previewQuantity =
    Number.isInteger(parsedQuantity) && parsedQuantity > 0 ? parsedQuantity : 0;
  const previewBalance = quickRow
    ? quick?.mode === 'add'
      ? quickRow.quantity + previewQuantity
      : quickRow.quantity - previewQuantity
    : 0;

  return (
    <>
      <PageHeader
        titleKey="warehouse.operations.title"
        descriptionKey="warehouse.operations.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            {session ? (
              <span className="status-badge status-badge--neutral">
                {t('warehouse.operations.roleBadge', { role: session.user.role })}
              </span>
            ) : null}
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
            <button
              type="button"
              className="button button--primary button--sm"
              onClick={() => setAddItemOpen(true)}
            >
              <Plus size={14} aria-hidden="true" />
              {t('warehouse.operations.addItem')}
            </button>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />
      <p className="operations-access-note">{t('warehouse.operations.demoAccessNotice')}</p>

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.operations.itemCount"
          value={formatNumber(rows.length)}
          icon={Package}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.operations.lowCount"
          value={formatNumber(lowCount)}
          icon={AlertTriangle}
          tone="warning"
        />
        <StatCard
          labelKey="warehouse.operations.outCount"
          value={formatNumber(outCount)}
          icon={AlertTriangle}
          tone="critical"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.operations.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={() => {
            setSearch('');
            setStatusFilter('all');
          }}
          filters={
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value as StatusFilter)}
              aria-label={t('warehouse.operations.statusFilter')}
              style={filterStyle}
            >
              {statusOptions.map((option) => (
                <option key={option} value={option}>
                  {option === 'all' ? t('warehouse.operations.allStatuses') : t(statusKeys[option])}
                </option>
              ))}
            </select>
          }
        />
      </div>

      <DataTable
        rows={filteredRows}
        columns={columns}
        rowKey={(row) => row.product.id}
        captionKey="warehouse.operations.title"
        emptyMessageKey="warehouse.operations.noProducts"
        className={`operations-table ${
          lowCount + outCount > 0 ? 'operations-table--has-alerts' : ''
        }`.trim()}
      />

      <ProductFormModal
        open={addItemOpen}
        product={null}
        warehouseId={selectedWarehouse.id}
        dataset={dataset}
        titleKey="warehouse.operations.addItemTitle"
        onClose={() => setAddItemOpen(false)}
        onSave={saveProduct}
      />

      <Modal
        open={quick !== null}
        onClose={closeQuick}
        titleKey={
          quick?.mode === 'issue'
            ? 'warehouse.operations.quickIssueTitle'
            : 'warehouse.operations.quickAddTitle'
        }
        titleValues={quick ? { product: productDisplayName(quick.product) } : undefined}
        descriptionKey={
          quick?.mode === 'issue'
            ? 'warehouse.operations.quickIssueDescription'
            : 'warehouse.operations.quickAddDescription'
        }
        size="md"
        footer={
          <>
            <button type="button" className="button button--quiet" onClick={closeQuick}>
              {t('common.actions.cancel')}
            </button>
            <button
              type="button"
              className="button button--primary"
              onClick={submitQuick}
              disabled={submitting}
            >
              {quick?.mode === 'issue'
                ? t('warehouse.operations.confirmIssue')
                : t('warehouse.operations.confirmAdd')}
            </button>
          </>
        }
      >
        {quick ? (
          <div className="operations-quick">
            <div className="operations-quick__product">
              <ProductAvatar product={quick.product} />
              <div>
                <strong>{quick.product.nameAr || quick.product.name}</strong>
                <small dir="ltr">{quick.product.sku}</small>
              </div>
              <span className="operations-quick__stock" dir="ltr">
                {formatNumber(quickRow?.quantity ?? 0)} {quickRow?.unitLabel ?? ''}
              </span>
            </div>
            <div className="operations-quick__meta">
              <span>
                {t('warehouse.operations.minimumQuantity')}:{' '}
                <b dir="ltr">{formatNumber(quickRow?.minimum ?? 0)}</b>
              </span>
              {quickRow && (quickRow.status === 'low' || quickRow.status === 'out') ? (
                <span className="status-badge status-badge--warning">
                  {t('warehouse.operations.lowBadge')}
                </span>
              ) : null}
            </div>
            <div className="form-field">
              <label className="form-field__label" htmlFor={quantityFieldId}>
                {t('warehouse.operations.quantityField')}
                <span className="form-field__required" aria-hidden="true">
                  *
                </span>
              </label>
              <div className="operations-stepper">
                <button
                  type="button"
                  className="button button--quiet button--sm"
                  onClick={() => changeQuantity(-1)}
                  aria-label={t('warehouse.operations.decrementQuantity')}
                >
                  −
                </button>
                <input
                  id={quantityFieldId}
                  type="number"
                  min="1"
                  dir="ltr"
                  value={quantity}
                  onChange={(event) => {
                    setQuantity(event.target.value);
                    setQuickError(null);
                  }}
                />
                <button
                  type="button"
                  className="button button--quiet button--sm"
                  onClick={() => changeQuantity(1)}
                  aria-label={t('warehouse.operations.incrementQuantity')}
                >
                  +
                </button>
                <span>{quickRow?.unitLabel ?? ''}</span>
              </div>
              {quickError ? (
                <span className="form-field__error" role="alert">
                  {t(quickError)}
                </span>
              ) : null}
            </div>
            <FormField labelKey="warehouse.operations.reasonField">
              <input
                value={reason}
                placeholder={t('warehouse.operations.reasonPlaceholder')}
                onChange={(event) => setReason(event.target.value)}
              />
            </FormField>
            <div className="operations-quick__preview">
              <span>{t('warehouse.operations.newBalance')}</span>
              <strong dir="ltr">
                {formatNumber(previewBalance)} {quickRow?.unitLabel ?? ''}
              </strong>
            </div>
          </div>
        ) : null}
      </Modal>
    </>
  );
}

export default WarehouseOperationsPage;
