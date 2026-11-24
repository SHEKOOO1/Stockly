import { Boxes, Eye, Layers3 } from 'lucide-react';
import { useMemo, useState } from 'react';
import {
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
import { demoCategories, demoUnits } from '@/data/demoData';
import { useDemoProductStore } from '@/data/demoProductStore';
import { t, type TranslationKey } from '@/i18n';
import { formatDate, formatNumber } from '@/lib/format';
import type { ProductSummary } from '@/lib/product';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { ProductStockStatus, StatusTone } from '@/types/domain';

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

const stockFilters: readonly ProductStockStatus[] = ['healthy', 'low', 'out', 'expiring'];

const stockLabels: Record<ProductStockStatus, TranslationKey> = {
  healthy: 'common.status.healthy',
  low: 'common.status.low',
  out: 'common.status.out',
  expiring: 'common.status.expiring',
};

const stockTones: Record<ProductStockStatus, StatusTone> = {
  healthy: 'healthy',
  low: 'warning',
  out: 'critical',
  expiring: 'warning',
};

export function StockOverviewPage() {
  const { selectedWarehouse } = useApp();
  const { productSummaries } = useDemoProductStore();
  const loader = useDemoLoader(productSummaries);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<ProductStockStatus | ''>('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [details, setDetails] = useState<ProductSummary | null>(null);

  const categories = useMemo(
    () => demoCategories.filter((category) => category.warehouseId === selectedWarehouse.id),
    [selectedWarehouse.id],
  );

  const rows = useMemo(
    () =>
      productSummaries.filter((summary) => summary.product.warehouseId === selectedWarehouse.id),
    [productSummaries, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return rows.filter((summary) => {
      const matchesSearch =
        needle === '' ||
        `${summary.product.name} ${summary.product.sku}`.toLocaleLowerCase().includes(needle);
      const matchesStatus = statusFilter === '' || summary.stockStatus === statusFilter;
      const matchesCategory =
        categoryFilter === '' || summary.product.categoryId === categoryFilter;
      return matchesSearch && matchesStatus && matchesCategory;
    });
  }, [rows, search, statusFilter, categoryFilter]);

  const totalQuantity = useMemo(
    () => rows.reduce((total, summary) => total + summary.onHandQuantity, 0),
    [rows],
  );

  const columns: DataTableColumn<ProductSummary>[] = [
    {
      id: 'product',
      headerKey: 'common.labels.product',
      cell: (row) => <strong>{row.product.name}</strong>,
      sortValue: (row) => row.product.name,
    },
    {
      id: 'sku',
      headerKey: 'warehouse.stock.skuColumn',
      cell: (row) => <span dir="ltr">{row.product.sku}</span>,
      ltr: true,
      sortValue: (row) => row.product.sku,
    },
    {
      id: 'category',
      headerKey: 'warehouse.stock.categoryColumn',
      cell: (row) =>
        demoCategories.find((category) => category.id === row.product.categoryId)?.name ??
        row.product.categoryId ??
        t('warehouse.products.noCategory'),
      sortValue: (row) => row.product.categoryId ?? '',
    },
    {
      id: 'unit',
      headerKey: 'warehouse.stock.unitColumn',
      cell: (row) =>
        demoUnits.find((unit) => unit.id === row.product.baseUnitId)?.code ??
        row.product.baseUnitId,
    },
    {
      id: 'quantity',
      headerKey: 'warehouse.stock.quantityColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.onHandQuantity)}</span>,
      sortValue: (row) => row.onHandQuantity,
      align: 'end',
      ltr: true,
    },
    {
      id: 'threshold',
      headerKey: 'warehouse.stock.thresholdColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.minimumQuantity)}</span>,
      sortValue: (row) => row.minimumQuantity,
      align: 'end',
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'warehouse.stock.statusColumn',
      cell: (row) => (
        <StatusBadge tone={stockTones[row.stockStatus]} labelKey={stockLabels[row.stockStatus]} />
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

  function resetFilters() {
    setSearch('');
    setStatusFilter('');
    setCategoryFilter('');
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.stock.title"
        descriptionKey="warehouse.stock.description"
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
          labelKey="warehouse.stock.trackedProducts"
          value={formatNumber(rows.length)}
          hintKey="warehouse.stock.trackedHint"
          icon={Layers3}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.stock.availableQuantity"
          value={formatNumber(totalQuantity)}
          hintKey="warehouse.stock.availableHint"
          icon={Boxes}
          tone="neutral"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.stock.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <>
              <select
                value={statusFilter}
                onChange={(event) => setStatusFilter(event.target.value as ProductStockStatus | '')}
                aria-label={t('warehouse.stock.statusFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {stockFilters.map((status) => (
                  <option key={status} value={status}>
                    {t(stockLabels[status])}
                  </option>
                ))}
              </select>
              <select
                value={categoryFilter}
                onChange={(event) => setCategoryFilter(event.target.value)}
                aria-label={t('warehouse.stock.categoryFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
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
          captionKey="warehouse.stock.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={details !== null}
        onClose={() => setDetails(null)}
        titleKey="warehouse.shared.detailsAction"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={statGridStyle}>
            <div>
              <strong>{t('common.labels.code')}</strong>
              <p dir="ltr">{details.product.sku}</p>
            </div>
            <div>
              <strong>{t('common.labels.quantity')}</strong>
              <p dir="ltr">{formatNumber(details.onHandQuantity)}</p>
            </div>
            <div>
              <strong>{t('warehouse.stock.thresholdColumn')}</strong>
              <p dir="ltr">{formatNumber(details.minimumQuantity)}</p>
            </div>
            <div>
              <strong>{t('common.labels.status')}</strong>
              <p>
                <StatusBadge
                  tone={stockTones[details.stockStatus]}
                  labelKey={stockLabels[details.stockStatus]}
                />
              </p>
            </div>
            <div>
              <strong>{t('warehouse.productDetails.updatedLabel')}</strong>
              <p dir="ltr">{formatDate(details.product.updatedAt)}</p>
            </div>
          </div>
        ) : null}
      </Modal>
    </>
  );
}

export default StockOverviewPage;
