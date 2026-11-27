import { Boxes, CalendarClock, Eye } from 'lucide-react';
import { useMemo, useState } from 'react';
import {
  DataTable,
  DemoNotice,
  EmptyState,
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
import { useDemoCategoryStore } from '@/data/demoCategoryStore';
import { useDemoProductStore } from '@/data/demoProductStore';
import { useDemoUnitStore } from '@/data/demoUnitStore';
import { t, type TranslationKey } from '@/i18n';
import { getCategoryDisplayName } from '@/lib/category';
import { getUnitOptionLabel } from '@/lib/unit';
import { formatDate, formatNumber } from '@/lib/format';
import type { ProductSummary } from '@/lib/product';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { ProductStockStatus, StatusTone } from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const detailGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
  gap: '16px',
} as const;

const actionRowStyle = {
  display: 'flex',
  justifyContent: 'flex-end',
} as const;

const filterSelectStyle = { minWidth: 160 } as const;

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

type ExpiryRisk = 'critical' | 'warning' | 'healthy';

const riskOptions: readonly ExpiryRisk[] = ['critical', 'warning', 'healthy'];

const riskLabels: Record<ExpiryRisk, TranslationKey> = {
  critical: 'warehouse.expiration.riskCritical',
  warning: 'warehouse.expiration.riskWarning',
  healthy: 'warehouse.expiration.riskHealthy',
};

const riskTones: Record<ExpiryRisk, StatusTone> = {
  critical: 'critical',
  warning: 'warning',
  healthy: 'healthy',
};

function expiryRisk(expiryDate: string | null): ExpiryRisk {
  if (!expiryDate) {
    return 'healthy';
  }
  const days = (new Date(expiryDate).getTime() - Date.now()) / 86_400_000;
  if (days <= 14) {
    return 'critical';
  }
  if (days <= 30) {
    return 'warning';
  }
  return 'healthy';
}

export function ExpirationTrackingPage() {
  const { selectedWarehouse } = useApp();
  const { productSummaries } = useDemoProductStore();
  const { getCategoryById } = useDemoCategoryStore();
  const { units } = useDemoUnitStore();
  const loader = useDemoLoader(productSummaries);
  const [search, setSearch] = useState('');
  const [riskFilter, setRiskFilter] = useState<ExpiryRisk | ''>('');
  const [details, setDetails] = useState<ProductSummary | null>(null);

  const rows = useMemo(
    () =>
      productSummaries.filter(
        (summary) =>
          summary.product.warehouseId === selectedWarehouse.id &&
          summary.product.isExpiryTracked &&
          summary.nearestExpiry?.expiryDate,
      ),
    [productSummaries, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return rows.filter((summary) => {
      const matchesSearch =
        needle === '' ||
        `${summary.product.name} ${summary.product.sku}`.toLocaleLowerCase().includes(needle);
      const matchesRisk =
        riskFilter === '' || expiryRisk(summary.nearestExpiry?.expiryDate ?? null) === riskFilter;
      return matchesSearch && matchesRisk;
    });
  }, [rows, search, riskFilter]);

  function unitLabel(unitId: string): string {
    const unit = units.find((item) => item.id === unitId);
    return unit ? getUnitOptionLabel(unit, t('warehouse.shared.archivedUnitSuffix')) : unitId;
  }

  const criticalCount = useMemo(
    () =>
      rows.filter((summary) => expiryRisk(summary.nearestExpiry?.expiryDate ?? null) === 'critical')
        .length,
    [rows],
  );

  const trackedQuantity = useMemo(
    () => rows.reduce((total, summary) => total + summary.onHandQuantity, 0),
    [rows],
  );

  const columns: DataTableColumn<ProductSummary>[] = [
    {
      id: 'product',
      headerKey: 'warehouse.expiration.productColumn',
      cell: (row) => <strong>{row.product.name}</strong>,
      sortValue: (row) => row.product.name,
    },
    {
      id: 'sku',
      headerKey: 'warehouse.expiration.skuColumn',
      cell: (row) => <span dir="ltr">{row.product.sku}</span>,
      sortValue: (row) => row.product.sku,
      ltr: true,
    },
    {
      id: 'expiry',
      headerKey: 'warehouse.expiration.expiryColumn',
      cell: (row) => <span dir="ltr">{formatDate(row.nearestExpiry?.expiryDate ?? '')}</span>,
      sortValue: (row) => row.nearestExpiry?.expiryDate ?? '',
      ltr: true,
    },
    {
      id: 'risk',
      headerKey: 'warehouse.expiration.riskColumn',
      cell: (row) => {
        const risk = expiryRisk(row.nearestExpiry?.expiryDate ?? null);
        return <StatusBadge tone={riskTones[risk]} labelKey={riskLabels[risk]} />;
      },
      sortValue: (row) => expiryRisk(row.nearestExpiry?.expiryDate ?? null),
    },
    {
      id: 'quantity',
      headerKey: 'warehouse.expiration.quantityColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.onHandQuantity)}</span>,
      sortValue: (row) => row.onHandQuantity,
      align: 'end',
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'warehouse.expiration.statusColumn',
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
    setRiskFilter('');
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.expiration.title"
        descriptionKey="warehouse.expiration.description"
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
          labelKey="warehouse.expiration.title"
          value={formatNumber(rows.length)}
          icon={CalendarClock}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.expiration.riskCritical"
          value={formatNumber(criticalCount)}
          icon={CalendarClock}
          tone="critical"
        />
        <StatCard
          labelKey="warehouse.stock.availableQuantity"
          value={formatNumber(trackedQuantity)}
          icon={Boxes}
          tone="healthy"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.expiration.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <select
              value={riskFilter}
              onChange={(event) => setRiskFilter(event.target.value as ExpiryRisk | '')}
              aria-label={t('warehouse.expiration.riskFilter')}
              style={filterSelectStyle}
            >
              <option value="">{t('common.labels.all')}</option>
              {riskOptions.map((risk) => (
                <option key={risk} value={risk}>
                  {t(riskLabels[risk])}
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
      ) : rows.length === 0 ? (
        <EmptyState descriptionKey="warehouse.expiration.noProducts" />
      ) : (
        <DataTable
          rows={filteredRows}
          columns={columns}
          rowKey={(row) => row.product.id}
          pageSize={7}
          captionKey="warehouse.expiration.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={details !== null}
        onClose={() => setDetails(null)}
        titleKey="warehouse.expiration.detailsTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={detailGridStyle}>
            <div>
              <strong>{t('warehouse.expiration.skuColumn')}</strong>
              <p dir="ltr">{details.product.sku}</p>
            </div>
            <div>
              <strong>{t('warehouse.expiration.expiryColumn')}</strong>
              <p dir="ltr">{formatDate(details.nearestExpiry?.expiryDate ?? '')}</p>
            </div>
            <div>
              <strong>{t('warehouse.expiration.riskColumn')}</strong>
              <p>
                <StatusBadge
                  tone={riskTones[expiryRisk(details.nearestExpiry?.expiryDate ?? null)]}
                  labelKey={riskLabels[expiryRisk(details.nearestExpiry?.expiryDate ?? null)]}
                />
              </p>
            </div>
            <div>
              <strong>{t('common.labels.category')}</strong>
              <p>
                {getCategoryDisplayName(
                  getCategoryById(details.product.categoryId),
                  t('warehouse.products.noCategory'),
                  t('warehouse.shared.archivedCategorySuffix'),
                )}
              </p>
            </div>
            <div>
              <strong>{t('common.labels.unit')}</strong>
              <p>{unitLabel(details.product.baseUnitId)}</p>
            </div>
          </div>
        ) : null}
      </Modal>
    </>
  );
}

export default ExpirationTrackingPage;
