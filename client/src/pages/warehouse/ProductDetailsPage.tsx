import { Boxes, CalendarClock, Layers3, Pencil, RotateCcw, Settings2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { ProductFormModal } from '@/components/products/ProductFormModal';
import {
  ConfirmDialog,
  DataTable,
  DemoNotice,
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
  StatusBadge,
  Tabs,
  type DataTableColumn,
  type TabItem,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { useDemoCategoryStore } from '@/data/demoCategoryStore';
import { useDemoInventoryStore } from '@/data/demoInventoryStore';
import { useDemoProductStore, type ProductFormValues } from '@/data/demoProductStore';
import { useDemoSupplierStore } from '@/data/demoSupplierStore';
import { useDemoUnitStore } from '@/data/demoUnitStore';
import { getCategoryDisplayName } from '@/lib/category';
import { getSupplierDisplayName } from '@/lib/supplier';
import { getUnitOptionLabel } from '@/lib/unit';
import { t, type TranslationKey } from '@/i18n';
import { formatDate, formatDateTime, formatNumber } from '@/lib/format';
import type { ProductDataset, ProductSummary } from '@/lib/product';
import { stockStatusLabelKeys, stockStatusTones } from '@/lib/productPresentation';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { Batch, InventoryMovement, MovementType } from '@/types/domain';

const movementLabels: Record<MovementType, TranslationKey> = {
  STOCK_IN: 'warehouse.movements.typeStockIn',
  STOCK_OUT: 'warehouse.movements.typeStockOut',
  TRANSFER: 'warehouse.movements.typeTransfer',
  ADJUSTMENT: 'warehouse.movements.typeAdjustment',
  COUNT: 'warehouse.movements.typeCount',
};

function defaultReasonLabel(reasonCodeId: string | null): string {
  switch (reasonCodeId) {
    case 'reason-count':
      return t('warehouse.products.reasonCount');
    case 'reason-damage':
      return t('warehouse.products.reasonDamage');
    case 'reason-correction':
      return t('warehouse.products.reasonCorrection');
    default:
      return t('warehouse.products.noReason');
  }
}

interface DefinitionItemProps {
  labelKey: TranslationKey;
  value: string;
  ltr?: boolean;
}

function DefinitionItem({ labelKey, value, ltr = false }: DefinitionItemProps) {
  return (
    <div className="product-definition__item">
      <span className="product-definition__label">{t(labelKey)}</span>
      <span className="product-definition__value" dir={ltr ? 'ltr' : undefined}>
        {value}
      </span>
    </div>
  );
}

export function ProductDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { selectedWarehouse, showToast } = useApp();
  const {
    products,
    barcodes,
    settings,
    balances,
    batches,
    getProductSummary,
    restoreProduct,
    updateProduct,
  } = useDemoProductStore();
  const { getCategoryById } = useDemoCategoryStore();
  const { units } = useDemoUnitStore();
  const { getSupplierById } = useDemoSupplierStore();
  const { movements } = useDemoInventoryStore();
  const loader = useDemoLoader(products, { delayMs: 200 });
  const [formOpen, setFormOpen] = useState(false);
  const [restoreOpen, setRestoreOpen] = useState(false);

  const product = useMemo(
    () =>
      products.find((item) => item.id === id && item.warehouseId === selectedWarehouse.id) ?? null,
    [products, id, selectedWarehouse.id],
  );

  const summary = useMemo<ProductSummary | null>(
    () => (product ? getProductSummary(product) : null),
    [product, getProductSummary],
  );

  const dataset = useMemo<ProductDataset>(
    () => ({ products, barcodes, settings, balances, batches }),
    [products, barcodes, settings, balances, batches],
  );

  const productMovements = useMemo(
    () =>
      movements
        .filter(
          (movement) => movement.productId === id && movement.warehouseId === selectedWarehouse.id,
        )
        .sort((left, right) => right.occurredAt.localeCompare(left.occurredAt)),
    [movements, id, selectedWarehouse.id],
  );

  function unitLabel(unitId: string | null | undefined): string {
    const unit = units.find((item) => item.id === unitId);
    return unit
      ? getUnitOptionLabel(unit, t('warehouse.shared.archivedUnitSuffix'))
      : t('common.states.notAvailable');
  }

  const batchColumns = useMemo<DataTableColumn<Batch>[]>(
    () => [
      {
        id: 'batch',
        headerKey: 'warehouse.productDetails.batchNumberLabel',
        cell: (row) => (
          <span dir="ltr" className="product-mono">
            {row.batchNumber}
          </span>
        ),
        sortValue: (row) => row.batchNumber,
        ltr: true,
      },
      {
        id: 'production',
        headerKey: 'warehouse.productDetails.productionDateLabel',
        cell: (row) => (
          <span dir="ltr">{row.productionDate ? formatDate(row.productionDate) : '—'}</span>
        ),
        sortValue: (row) => row.productionDate ?? '',
        ltr: true,
      },
      {
        id: 'expiry',
        headerKey: 'warehouse.productDetails.expiryDateLabel',
        cell: (row) => <span dir="ltr">{row.expiryDate ? formatDate(row.expiryDate) : '—'}</span>,
        sortValue: (row) => row.expiryDate ?? '',
        ltr: true,
      },
    ],
    [],
  );

  const movementColumns = useMemo<DataTableColumn<InventoryMovement>[]>(
    () => [
      {
        id: 'type',
        headerKey: 'warehouse.productDetails.typeColumn',
        cell: (row) => t(movementLabels[row.type]),
      },
      {
        id: 'date',
        headerKey: 'warehouse.productDetails.dateColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
        sortValue: (row) => row.occurredAt,
        ltr: true,
      },
      {
        id: 'quantity',
        headerKey: 'warehouse.productDetails.quantityColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.quantity)}</span>,
        sortValue: (row) => row.quantity,
        align: 'end',
        ltr: true,
      },
      {
        id: 'transaction',
        headerKey: 'warehouse.productDetails.transactionColumn',
        cell: (row) => <span dir="ltr">{row.relatedTransaction}</span>,
        ltr: true,
      },
      {
        id: 'notes',
        headerKey: 'warehouse.productDetails.notesColumn',
        cell: (row) => row.notes ?? '—',
        sortValue: (row) => row.notes ?? '',
      },
    ],
    [],
  );

  function handleSave(values: ProductFormValues) {
    if (!product || product.isDeleted) {
      return;
    }
    updateProduct(product.id, values);
    setFormOpen(false);
    showToast('warehouse.shared.updateToast', 'info');
  }

  function handleRestore() {
    if (!product) {
      return;
    }
    restoreProduct(product.id);
    setRestoreOpen(false);
    showToast('warehouse.shared.statusToast', 'info');
  }

  const tabs: readonly TabItem[] = [
    {
      id: 'overview',
      labelKey: 'warehouse.productDetails.overviewTab',
      content: summary ? (
        <section className="product-section">
          <h3 className="product-section__title">
            <Layers3 size={16} aria-hidden="true" />
            {t('warehouse.productDetails.overviewPanel')}
          </h3>
          <div className="product-definition">
            <DefinitionItem
              labelKey="warehouse.productDetails.categoryLabel"
              value={getCategoryDisplayName(
                getCategoryById(summary.product.categoryId),
                t('warehouse.products.noCategory'),
                t('warehouse.shared.archivedCategorySuffix'),
              )}
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.unitLabel"
              value={unitLabel(summary.product.baseUnitId)}
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.skuLabel"
              value={summary.product.sku}
              ltr
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.nameArLabel"
              value={summary.product.nameAr ?? '—'}
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.primaryBarcodeLabel"
              value={summary.barcode?.barcode ?? '—'}
              ltr
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.purchaseUnitLabel"
              value={unitLabel(summary.product.purchaseUnitId)}
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.batchTrackingLabel"
              value={
                summary.product.isBatchTracked
                  ? t('common.status.active')
                  : t('common.status.inactive')
              }
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.expiryTrackingLabel"
              value={
                summary.product.isExpiryTracked
                  ? t('common.status.active')
                  : t('common.status.inactive')
              }
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.shelfLifeLabel"
              value={
                summary.shelfLifeDays === null
                  ? t('common.states.notAvailable')
                  : formatNumber(summary.shelfLifeDays)
              }
              ltr
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.defaultReasonLabel"
              value={defaultReasonLabel(summary.product.defaultReasonCodeId)}
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.lifecycleLabel"
              value={
                summary.product.isDeleted
                  ? t('warehouse.products.archivedLifecycle')
                  : summary.product.isActive
                    ? t('warehouse.products.activeLifecycle')
                    : t('common.status.inactive')
              }
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.updatedLabel"
              value={formatDateTime(summary.product.updatedAt)}
              ltr
            />
            <DefinitionItem
              labelKey="warehouse.productDetails.descriptionLabel"
              value={summary.product.description ?? '—'}
            />
          </div>
        </section>
      ) : (
        <EmptyState descriptionKey="warehouse.productDetails.notFoundDescription" />
      ),
    },
    {
      id: 'stock',
      labelKey: 'warehouse.productDetails.stockTab',
      content: summary ? (
        <>
          <div className="product-stock-band">
            <div className="product-stock-band__item">
              <span>{t('warehouse.productDetails.onHandLabel')}</span>
              <strong dir="ltr">{formatNumber(summary.onHandQuantity)}</strong>
            </div>
            <div className="product-stock-band__item">
              <span>{t('warehouse.productDetails.incomingLabel')}</span>
              <strong dir="ltr">{formatNumber(summary.incomingQuantity)}</strong>
            </div>
            <div className="product-stock-band__item">
              <span>{t('warehouse.productDetails.minimumLabel')}</span>
              <strong dir="ltr">{formatNumber(summary.minimumQuantity)}</strong>
            </div>
            <div className="product-stock-band__item">
              <span>{t('warehouse.productDetails.reorderLabel')}</span>
              <strong dir="ltr">{formatNumber(summary.reorderQuantity ?? 0)}</strong>
            </div>
          </div>
          <section className="product-section">
            <h3 className="product-section__title">
              <Settings2 size={16} aria-hidden="true" />
              {t('warehouse.productDetails.stockPanel')}
            </h3>
            <div className="product-definition">
              <DefinitionItem
                labelKey="warehouse.productDetails.maximumLabel"
                value={formatNumber(summary.maximumQuantity ?? 0)}
                ltr
              />
              <DefinitionItem
                labelKey="warehouse.productDetails.supplierLabel"
                value={getSupplierDisplayName(
                  getSupplierById(summary.setting?.defaultSupplierId),
                  t('warehouse.products.noSupplier'),
                  t('warehouse.suppliers.archivedSuffix'),
                )}
              />
              <DefinitionItem
                labelKey="warehouse.products.stockStatusColumn"
                value={t(stockStatusLabelKeys[summary.stockStatus])}
              />
              <DefinitionItem
                labelKey="warehouse.products.nearestExpiry"
                value={
                  summary.nearestExpiry?.expiryDate
                    ? formatDate(summary.nearestExpiry.expiryDate)
                    : t('warehouse.products.notExpiring')
                }
                ltr
              />
            </div>
          </section>
          <section className="product-section">
            <h3 className="product-section__title">
              <CalendarClock size={16} aria-hidden="true" />
              {t('warehouse.productDetails.batchesLabel')}
            </h3>
            {summary.batches.length > 0 ? (
              <DataTable
                rows={summary.batches}
                columns={batchColumns}
                rowKey={(row) => row.id}
                pageSize={5}
                captionKey="warehouse.productDetails.batchesLabel"
                emptyMessageKey="warehouse.productDetails.noBatches"
              />
            ) : (
              <EmptyState descriptionKey="warehouse.productDetails.noBatches" />
            )}
          </section>
        </>
      ) : (
        <EmptyState descriptionKey="warehouse.productDetails.notFoundDescription" />
      ),
    },
    {
      id: 'movements',
      labelKey: 'warehouse.productDetails.movementsTab',
      content:
        productMovements.length > 0 ? (
          <DataTable
            rows={productMovements}
            columns={movementColumns}
            rowKey={(row) => row.id}
            pageSize={6}
            captionKey="warehouse.productDetails.movementsPanel"
            emptyMessageKey="warehouse.productDetails.noMovements"
          />
        ) : (
          <EmptyState descriptionKey="warehouse.productDetails.noMovements" />
        ),
    },
    {
      id: 'audit',
      labelKey: 'warehouse.productDetails.auditTab',
      content: <EmptyState descriptionKey="warehouse.productDetails.auditEmpty" />,
    },
  ];

  return (
    <>
      <PageHeader
        titleKey="warehouse.productDetails.title"
        descriptionKey="warehouse.productDetails.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
            {summary ? (
              <StatusBadge
                tone={stockStatusTones[summary.stockStatus]}
                labelKey={stockStatusLabelKeys[summary.stockStatus]}
              />
            ) : null}
            {product?.isDeleted ? (
              <StatusBadge tone="neutral" labelKey="warehouse.products.archivedLifecycle" />
            ) : null}
            {product?.isDeleted ? (
              <button
                type="button"
                className="button button--quiet button--sm"
                onClick={() => setRestoreOpen(true)}
              >
                <RotateCcw size={14} aria-hidden="true" />
                {t('warehouse.products.restoreAction')}
              </button>
            ) : null}
            <button
              type="button"
              className="button button--quiet button--sm"
              disabled={!product || product.isDeleted}
              onClick={() => setFormOpen(true)}
            >
              <Pencil size={14} aria-hidden="true" />
              {t('warehouse.products.editAction')}
            </button>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      {summary ? (
        <div className="product-detail-grid">
          <section className="product-section">
            <h2 className="product-section__title">
              <Boxes size={16} aria-hidden="true" />
              {summary.product.name}
            </h2>
            <div className="product-chip-row">
              <span dir="ltr" className="product-mono">
                {summary.product.sku}
              </span>
              <span dir="ltr" className="product-mono">
                {summary.barcode?.barcode ?? t('common.states.notAvailable')}
              </span>
              <span className="product-note">
                {summary.product.nameAr ?? t('common.states.notAvailable')}
              </span>
            </div>
          </section>
          <section className="product-section">
            <h2 className="product-section__title">
              <CalendarClock size={16} aria-hidden="true" />
              {t('warehouse.products.expiryColumn')}
            </h2>
            <div className="product-chip-row">
              <span dir="ltr" className="product-mono">
                {summary.nearestExpiry?.expiryDate
                  ? formatDate(summary.nearestExpiry.expiryDate)
                  : t('warehouse.products.notExpiring')}
              </span>
              {summary.nearestExpiry ? (
                <span className="status-badge status-badge--warning">
                  {t('warehouse.products.expiringSoon')}
                </span>
              ) : null}
            </div>
          </section>
        </div>
      ) : null}

      {loader.status === 'error' ? (
        <ErrorState onRetry={loader.reload} />
      ) : loader.status === 'loading' ? (
        <LoadingState />
      ) : (
        <Tabs items={tabs} defaultValue="overview" />
      )}

      <ProductFormModal
        open={formOpen}
        product={product}
        warehouseId={selectedWarehouse.id}
        dataset={dataset}
        onClose={() => setFormOpen(false)}
        onSave={handleSave}
      />
      <ConfirmDialog
        open={restoreOpen}
        onClose={() => setRestoreOpen(false)}
        onConfirm={handleRestore}
        titleKey="warehouse.products.restoreTitle"
        messageKey="warehouse.products.restoreMessage"
        confirmLabelKey="warehouse.products.restoreAction"
      />
    </>
  );
}

export default ProductDetailsPage;
