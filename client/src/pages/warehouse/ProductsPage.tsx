import { CalendarClock, ClipboardList, Package, Plus, TriangleAlert } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ProductFormModal } from '@/components/products/ProductFormModal';
import { ProductRowActions } from '@/components/products/ProductRowActions';
import {
  ConfirmDialog,
  DataTable,
  DemoNotice,
  ErrorState,
  LoadingState,
  PageHeader,
  SearchFilterBar,
  StatusBadge,
  type DataTableColumn,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { useDemoCategoryStore } from '@/data/demoCategoryStore';
import { useDemoProductStore, type ProductFormValues } from '@/data/demoProductStore';
import { useDemoSupplierStore } from '@/data/demoSupplierStore';
import { useDemoUnitStore } from '@/data/demoUnitStore';
import { locale, t } from '@/i18n';
import { getCategoryDisplayName } from '@/lib/category';
import { getSupplierDisplayName } from '@/lib/supplier';
import { getUnitDisplayName } from '@/lib/unit';
import { formatDate, formatNumber } from '@/lib/format';
import type { ProductDataset, ProductSummary } from '@/lib/product';
import { stockStatusLabelKeys, stockStatusTones } from '@/lib/productPresentation';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { Product, ProductStockStatus } from '@/types/domain';

const stockFilters: readonly ProductStockStatus[] = ['healthy', 'low', 'out', 'expiring'];

interface ProductHealthProps {
  icon: typeof Package;
  labelKey:
    | 'warehouse.products.healthTotal'
    | 'warehouse.products.healthLow'
    | 'warehouse.products.healthOut'
    | 'warehouse.products.healthExpiring';
  noteKey:
    | 'warehouse.products.healthTotalHint'
    | 'warehouse.products.healthLowHint'
    | 'warehouse.products.healthOutHint'
    | 'warehouse.products.healthExpiringHint';
  value: number;
  tone: '' | 'product-health--low' | 'product-health--out' | 'product-health--expiring';
}

function ProductHealth({ icon: Icon, labelKey, noteKey, value, tone }: ProductHealthProps) {
  return (
    <article className={`product-health ${tone}`.trim()}>
      <span className="product-health__label">{t(labelKey)}</span>
      <span className="product-health__value">{formatNumber(value)}</span>
      <span className="product-health__note">{t(noteKey)}</span>
      <span className="sr-only">
        <Icon aria-hidden="true" />
      </span>
    </article>
  );
}

export function ProductsPage() {
  const navigate = useNavigate();
  const { selectedWarehouse, showToast } = useApp();
  const {
    products,
    barcodes,
    settings,
    balances,
    batches,
    archiveProduct,
    addProduct,
    getProductSummary,
    restoreProduct,
    updateProduct,
  } = useDemoProductStore();
  const loader = useDemoLoader(products);
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [stockFilter, setStockFilter] = useState<ProductStockStatus | ''>('');
  const [lifecycleFilter, setLifecycleFilter] = useState<'active' | 'archived'>('active');
  const [supplierFilter, setSupplierFilter] = useState('');
  const [expiryFilter, setExpiryFilter] = useState<'tracked' | 'expiring' | ''>('');
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Product | null>(null);
  const [archiving, setArchiving] = useState<Product | null>(null);
  const [restoring, setRestoring] = useState<Product | null>(null);

  const { getCategorySummaries } = useDemoCategoryStore();
  const { units } = useDemoUnitStore();
  const { getSupplierSummaries } = useDemoSupplierStore();
  const categorySummaries = useMemo(
    () => getCategorySummaries(selectedWarehouse.id),
    [getCategorySummaries, selectedWarehouse.id],
  );
  const categories = useMemo(
    () => categorySummaries.map((row) => row.category),
    [categorySummaries],
  );
  const suppliers = useMemo(
    () => getSupplierSummaries(selectedWarehouse.organizationId).map((row) => row.supplier),
    [getSupplierSummaries, selectedWarehouse.organizationId],
  );
  const dataset = useMemo<ProductDataset>(
    () => ({ products, barcodes, settings, balances, batches }),
    [products, barcodes, settings, balances, batches],
  );
  const warehouseProducts = useMemo(
    () => products.filter((product) => product.warehouseId === selectedWarehouse.id),
    [products, selectedWarehouse.id],
  );

  const summaries = useMemo(
    () => warehouseProducts.map((product) => getProductSummary(product)),
    [warehouseProducts, getProductSummary],
  );

  const activeSummaries = useMemo(
    () => summaries.filter((summary) => !summary.product.isDeleted),
    [summaries],
  );

  const health = useMemo(
    () => ({
      total: activeSummaries.length,
      low: activeSummaries.filter((summary) => summary.stockStatus === 'low').length,
      out: activeSummaries.filter((summary) => summary.stockStatus === 'out').length,
      expiring: activeSummaries.filter((summary) => summary.stockStatus === 'expiring').length,
    }),
    [activeSummaries],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase(locale);
    return summaries.filter((summary) => {
      const { product } = summary;
      if (lifecycleFilter === 'active' ? product.isDeleted : !product.isDeleted) {
        return false;
      }
      const matchesSearch =
        needle === '' ||
        [product.name, product.nameAr ?? '', product.sku]
          .join(' ')
          .toLocaleLowerCase(locale)
          .includes(needle);
      const matchesCategory = categoryFilter === '' || product.categoryId === categoryFilter;
      const matchesStock = stockFilter === '' || summary.stockStatus === stockFilter;
      const matchesSupplier =
        supplierFilter === '' || summary.setting?.defaultSupplierId === supplierFilter;
      const matchesExpiry =
        expiryFilter === '' ||
        (expiryFilter === 'tracked' && product.isExpiryTracked) ||
        (expiryFilter === 'expiring' && Boolean(summary.nearestExpiry));
      return matchesSearch && matchesCategory && matchesStock && matchesSupplier && matchesExpiry;
    });
  }, [
    summaries,
    search,
    categoryFilter,
    stockFilter,
    lifecycleFilter,
    supplierFilter,
    expiryFilter,
  ]);

  const columns = useMemo<DataTableColumn<ProductSummary>[]>(
    () => [
      {
        id: 'identity',
        headerKey: 'warehouse.products.nameColumn',
        cell: (row) => (
          <div className="product-identity">
            <span className="product-identity__mark" aria-hidden="true">
              <Package size={17} />
            </span>
            <span className="product-identity__text">
              <strong className="product-identity__name">{row.product.name}</strong>
              <span className="product-identity__meta">
                {row.product.nameAr ?? t('common.states.notAvailable')}
              </span>
            </span>
          </div>
        ),
        sortValue: (row) => row.product.name,
      },
      {
        id: 'sku',
        headerKey: 'warehouse.products.skuColumn',
        cell: (row) => (
          <span dir="ltr" className="product-mono">
            {row.product.sku}
          </span>
        ),
        sortValue: (row) => row.product.sku,
        ltr: true,
      },
      {
        id: 'barcode',
        headerKey: 'warehouse.products.barcodeColumn',
        cell: (row) => (
          <span dir="ltr" className="product-mono">
            {row.barcode?.barcode ?? t('common.states.notAvailable')}
          </span>
        ),
        sortValue: (row) => row.barcode?.barcode ?? '',
        ltr: true,
      },
      {
        id: 'category',
        headerKey: 'warehouse.products.categoryColumn',
        cell: (row) =>
          getCategoryDisplayName(
            categories.find((category) => category.id === row.product.categoryId),
            t('warehouse.products.noCategory'),
            t('warehouse.shared.archivedCategorySuffix'),
          ),
        sortValue: (row) =>
          categories.find((category) => category.id === row.product.categoryId)?.name ?? '',
      },
      {
        id: 'supplier',
        headerKey: 'warehouse.products.supplierColumn',
        cell: (row) =>
          getSupplierDisplayName(
            suppliers.find((supplier) => supplier.id === row.setting?.defaultSupplierId),
            t('warehouse.products.noSupplier'),
            t('warehouse.suppliers.archivedSuffix'),
          ),
        sortValue: (row) =>
          suppliers.find((supplier) => supplier.id === row.setting?.defaultSupplierId)?.name ?? '',
      },
      {
        id: 'baseUnit',
        headerKey: 'warehouse.products.baseUnitColumn',
        cell: (row) =>
          getUnitDisplayName(
            units.find((unit) => unit.id === row.product.baseUnitId),
            t('common.states.notAvailable'),
            t('warehouse.shared.archivedUnitSuffix'),
          ),
        sortValue: (row) =>
          units.find((unit) => unit.id === row.product.baseUnitId)?.name ?? row.product.baseUnitId,
      },
      {
        id: 'onHand',
        headerKey: 'warehouse.products.onHandColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.onHandQuantity)}</span>,
        sortValue: (row) => row.onHandQuantity,
        align: 'end',
        ltr: true,
      },
      {
        id: 'minimum',
        headerKey: 'warehouse.products.minimumColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.minimumQuantity)}</span>,
        sortValue: (row) => row.minimumQuantity,
        align: 'end',
        ltr: true,
      },
      {
        id: 'stock',
        headerKey: 'warehouse.products.stockStatusColumn',
        cell: (row) => (
          <div className="product-status">
            <StatusBadge
              tone={stockStatusTones[row.stockStatus]}
              labelKey={stockStatusLabelKeys[row.stockStatus]}
            />
            <span className="product-status__note">
              {row.product.isBatchTracked
                ? t('warehouse.products.batchTracked')
                : t('warehouse.products.batchNotTracked')}
            </span>
          </div>
        ),
        sortValue: (row) => row.stockStatus,
      },
      {
        id: 'expiry',
        headerKey: 'warehouse.products.expiryColumn',
        cell: (row) => {
          if (!row.product.isExpiryTracked) {
            return t('warehouse.products.withoutExpiry');
          }
          const expiry = row.nearestExpiry?.expiryDate;
          return (
            <div className="product-status">
              <span dir="ltr" className="product-mono">
                {expiry ? formatDate(expiry) : t('warehouse.products.noExpiryDate')}
              </span>
              <span className="product-status__note">
                {row.nearestExpiry
                  ? t('warehouse.products.expiringSoon')
                  : t('warehouse.products.notExpiring')}
              </span>
            </div>
          );
        },
        sortValue: (row) => row.nearestExpiry?.expiryDate ?? '',
        ltr: true,
      },
      {
        id: 'lifecycle',
        headerKey: 'warehouse.products.lifecycleColumn',
        cell: (row) => (
          <StatusBadge
            tone={row.product.isDeleted ? 'neutral' : 'healthy'}
            labelKey={
              row.product.isDeleted
                ? 'warehouse.products.archivedLifecycle'
                : 'warehouse.products.activeLifecycle'
            }
          />
        ),
        sortValue: (row) => (row.product.isDeleted ? 'archived' : 'active'),
      },
      {
        id: 'actions',
        headerKey: 'common.table.actions',
        cell: (row) => (
          <ProductRowActions
            archived={row.product.isDeleted}
            onView={() => navigate(`/warehouse/products/${row.product.id}`)}
            onEdit={() => {
              setEditing(row.product);
              setFormOpen(true);
            }}
            onArchive={() => setArchiving(row.product)}
            onRestore={() => setRestoring(row.product)}
          />
        ),
      },
    ],
    [categories, navigate, suppliers, units],
  );

  function resetFilters() {
    setSearch('');
    setCategoryFilter('');
    setStockFilter('');
    setLifecycleFilter('active');
    setSupplierFilter('');
    setExpiryFilter('');
  }

  function openAdd() {
    setEditing(null);
    setFormOpen(true);
  }

  function handleSave(values: ProductFormValues) {
    if (editing) {
      updateProduct(editing.id, values);
      showToast('warehouse.shared.updateToast', 'info');
    } else {
      addProduct(selectedWarehouse.id, values);
      showToast('warehouse.shared.createToast', 'info');
    }
    setEditing(null);
    setFormOpen(false);
  }

  function confirmArchive() {
    if (!archiving) {
      return;
    }
    archiveProduct(archiving.id);
    showToast('warehouse.shared.statusToast', 'info');
    setArchiving(null);
  }

  function confirmRestore() {
    if (!restoring) {
      return;
    }
    restoreProduct(restoring.id);
    showToast('warehouse.shared.statusToast', 'info');
    setRestoring(null);
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.products.title"
        descriptionKey="warehouse.products.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
            <button type="button" className="button button--primary button--sm" onClick={openAdd}>
              <Plus size={14} aria-hidden="true" />
              {t('warehouse.products.addProduct')}
            </button>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      <div className="product-health-strip">
        <ProductHealth
          icon={Package}
          labelKey="warehouse.products.healthTotal"
          noteKey="warehouse.products.healthTotalHint"
          value={health.total}
          tone=""
        />
        <ProductHealth
          icon={TriangleAlert}
          labelKey="warehouse.products.healthLow"
          noteKey="warehouse.products.healthLowHint"
          value={health.low}
          tone="product-health--low"
        />
        <ProductHealth
          icon={ClipboardList}
          labelKey="warehouse.products.healthOut"
          noteKey="warehouse.products.healthOutHint"
          value={health.out}
          tone="product-health--out"
        />
        <ProductHealth
          icon={CalendarClock}
          labelKey="warehouse.products.healthExpiring"
          noteKey="warehouse.products.healthExpiringHint"
          value={health.expiring}
          tone="product-health--expiring"
        />
      </div>

      <div className="product-toolbar">
        <div className="product-toolbar__title">
          <strong>{t('warehouse.products.title')}</strong>
          <span>
            {lifecycleFilter === 'active'
              ? t('warehouse.products.activeOnly')
              : t('warehouse.products.archivedOnly')}
          </span>
        </div>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.products.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <>
              <select
                value={categoryFilter}
                onChange={(event) => setCategoryFilter(event.target.value)}
                aria-label={t('warehouse.products.categoryFilter')}
              >
                <option value="">{t('common.labels.all')}</option>
                {categorySummaries
                  .filter((row) => !row.category.isDeleted)
                  .map((row) => (
                    <option key={row.category.id} value={row.category.id}>
                      {row.category.name}
                    </option>
                  ))}
              </select>
              <select
                value={stockFilter}
                onChange={(event) => setStockFilter(event.target.value as ProductStockStatus | '')}
                aria-label={t('warehouse.products.stockStatusFilter')}
              >
                <option value="">{t('common.labels.all')}</option>
                {stockFilters.map((status) => (
                  <option key={status} value={status}>
                    {t(stockStatusLabelKeys[status])}
                  </option>
                ))}
              </select>
              <select
                value={lifecycleFilter}
                onChange={(event) =>
                  setLifecycleFilter(event.target.value as 'active' | 'archived')
                }
                aria-label={t('warehouse.products.lifecycleFilter')}
              >
                <option value="active">{t('warehouse.products.activeLifecycle')}</option>
                <option value="archived">{t('warehouse.products.archivedLifecycle')}</option>
              </select>
              <select
                value={supplierFilter}
                onChange={(event) => setSupplierFilter(event.target.value)}
                aria-label={t('warehouse.products.supplierFilter')}
              >
                <option value="">{t('common.labels.all')}</option>
                {suppliers.map((supplier) => (
                  <option key={supplier.id} value={supplier.id}>
                    {supplier.name}
                  </option>
                ))}
              </select>
              <select
                value={expiryFilter}
                onChange={(event) =>
                  setExpiryFilter(event.target.value as 'tracked' | 'expiring' | '')
                }
                aria-label={t('warehouse.products.expiryFilter')}
              >
                <option value="">{t('common.labels.all')}</option>
                <option value="tracked">{t('warehouse.products.expiryTracked')}</option>
                <option value="expiring">{t('warehouse.products.expiringSoon')}</option>
              </select>
            </>
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
          rowKey={(row) => row.product.id}
          pageSize={8}
          captionKey="warehouse.products.title"
          emptyMessageKey="warehouse.products.emptyRows"
          className="product-table"
          stickyHeader
        />
      )}

      <ProductFormModal
        open={formOpen}
        product={editing}
        warehouseId={selectedWarehouse.id}
        dataset={dataset}
        onClose={() => {
          setFormOpen(false);
          setEditing(null);
        }}
        onSave={handleSave}
      />

      <ConfirmDialog
        open={archiving !== null}
        onClose={() => setArchiving(null)}
        onConfirm={confirmArchive}
        titleKey="warehouse.products.archiveTitle"
        messageKey="warehouse.products.archiveMessage"
        confirmLabelKey="warehouse.products.archiveAction"
        tone="danger"
      />
      <ConfirmDialog
        open={restoring !== null}
        onClose={() => setRestoring(null)}
        onConfirm={confirmRestore}
        titleKey="warehouse.products.restoreTitle"
        messageKey="warehouse.products.restoreMessage"
        confirmLabelKey="warehouse.products.restoreAction"
      />
    </>
  );
}
