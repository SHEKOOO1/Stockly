import { Activity, Boxes, Tags, TriangleAlert } from 'lucide-react';
import { useMemo } from 'react';
import {
  BarChart,
  ChartCard,
  DataTable,
  DemoNotice,
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
  StatCard,
  StatusBadge,
  type DataTableColumn,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoActivities, demoAlerts, demoCategories, demoProducts } from '@/data/demoData';
import { useDemoProductStore } from '@/data/demoProductStore';
import { t, type TranslationKey } from '@/i18n';
import { formatDate, formatDateTime, formatNumber } from '@/lib/format';
import type { ProductSummary } from '@/lib/product';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type {
  Activity as ActivityRecord,
  ChartPoint,
  DemoAlert,
  EntityStatus,
  StatusTone,
} from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(210px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const panelGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(350px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const toneLabels: Record<StatusTone, TranslationKey> = {
  healthy: 'common.status.healthy',
  warning: 'common.status.warning',
  critical: 'common.status.critical',
  neutral: 'common.status.neutral',
  info: 'common.status.info',
};

const entityStatusLabels: Record<EntityStatus, TranslationKey> = {
  active: 'common.status.active',
  inactive: 'common.status.inactive',
  draft: 'common.status.draft',
  pending: 'common.status.pending',
  posted: 'common.status.posted',
  confirmed: 'common.status.confirmed',
};

export function OverviewPage() {
  const { selectedWarehouse } = useApp();
  const { productSummaries } = useDemoProductStore();
  const productsLoader = useDemoLoader(productSummaries, { delayMs: 190 });
  const activityLoader = useDemoLoader(demoActivities, { delayMs: 240 });
  const alertsLoader = useDemoLoader(demoAlerts, { delayMs: 210 });

  const products = useMemo<ProductSummary[]>(
    () =>
      productSummaries.filter((summary) => summary.product.warehouseId === selectedWarehouse.id),
    [productSummaries, selectedWarehouse.id],
  );

  const categories = useMemo(
    () => demoCategories.filter((category) => category.warehouseId === selectedWarehouse.id),
    [selectedWarehouse.id],
  );

  const warehouseAlerts = useMemo(
    () => demoAlerts.filter((alert) => alert.warehouseId === selectedWarehouse.id),
    [selectedWarehouse.id],
  );

  const warehouseActivity = useMemo(
    () =>
      demoActivities
        .filter((activity) => activity.warehouseId === selectedWarehouse.id)
        .sort((left, right) => right.occurredAt.localeCompare(left.occurredAt)),
    [selectedWarehouse.id],
  );

  const totalQuantity = useMemo(
    () => products.reduce((total, summary) => total + summary.onHandQuantity, 0),
    [products],
  );

  const categoryMix = useMemo<ChartPoint[]>(
    () =>
      categories.map((category) => ({
        id: category.id,
        label: category.name,
        value: products.filter((summary) => summary.product.categoryId === category.id).length,
        tone: 'info',
      })),
    [categories, products],
  );

  const activityColumns = useMemo<DataTableColumn<ActivityRecord>[]>(
    () => [
      {
        id: 'action',
        headerKey: 'warehouse.overview.actionColumn',
        cell: (row) => <strong>{row.action}</strong>,
        sortValue: (row) => row.action,
      },
      {
        id: 'actor',
        headerKey: 'warehouse.overview.actorColumn',
        cell: (row) => row.actor,
        sortValue: (row) => row.actor,
      },
      {
        id: 'tone',
        headerKey: 'common.labels.status',
        cell: (row) => <StatusBadge tone={row.tone} labelKey={toneLabels[row.tone]} />,
      },
      {
        id: 'time',
        headerKey: 'warehouse.overview.timeColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
        sortValue: (row) => row.occurredAt,
        ltr: true,
      },
    ],
    [],
  );

  const alertColumns = useMemo<DataTableColumn<DemoAlert>[]>(
    () => [
      {
        id: 'title',
        headerKey: 'warehouse.alerts.titleColumn',
        cell: (row) => <strong>{row.title}</strong>,
        sortValue: (row) => row.title,
      },
      {
        id: 'product',
        headerKey: 'common.labels.product',
        cell: (row) =>
          demoProducts.find((product) => product.id === row.productId)?.name ?? row.productId,
        sortValue: (row) =>
          demoProducts.find((product) => product.id === row.productId)?.name ?? row.productId,
      },
      {
        id: 'severity',
        headerKey: 'warehouse.alerts.severityColumn',
        cell: (row) => <StatusBadge tone={row.severity} labelKey={toneLabels[row.severity]} />,
      },
      {
        id: 'status',
        headerKey: 'common.labels.status',
        cell: (row) => <StatusBadge tone="info" labelKey={entityStatusLabels[row.status]} />,
      },
      {
        id: 'detected',
        headerKey: 'warehouse.alerts.detectedColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.detectedAt)}</span>,
        sortValue: (row) => row.detectedAt,
        ltr: true,
      },
    ],
    [],
  );

  const isLoading =
    productsLoader.status === 'loading' ||
    activityLoader.status === 'loading' ||
    alertsLoader.status === 'loading';
  const hasError =
    productsLoader.status === 'error' ||
    activityLoader.status === 'error' ||
    alertsLoader.status === 'error';

  return (
    <>
      <PageHeader
        titleKey="warehouse.overview.title"
        descriptionKey="warehouse.overview.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
          </>
        }
      />
      <DemoNotice compact />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.overview.availableQuantityKpi"
          value={formatNumber(totalQuantity)}
          hintKey="warehouse.overview.categoryMixSubtitle"
          icon={Boxes}
          tone="healthy"
        />
        <StatCard
          labelKey="warehouse.overview.categoriesKpi"
          value={formatNumber(categories.length)}
          hintKey="warehouse.products.categoryFilter"
          icon={Tags}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.overview.alertsKpi"
          value={formatNumber(
            warehouseAlerts.filter(
              (alert) => alert.status === 'active' || alert.status === 'pending',
            ).length,
          )}
          hintKey="warehouse.overview.alertsSubtitle"
          icon={TriangleAlert}
          tone="warning"
        />
        <StatCard
          labelKey="warehouse.overview.lastActivityKpi"
          value={formatDate(selectedWarehouse.lastActivityAt)}
          hintKey="warehouse.overview.activitySubtitle"
          icon={Activity}
          tone="neutral"
        />
      </div>

      {hasError ? (
        <ErrorState
          onRetry={() => {
            productsLoader.reload();
            activityLoader.reload();
            alertsLoader.reload();
          }}
        />
      ) : isLoading ? (
        <LoadingState />
      ) : (
        <>
          <div style={panelGridStyle}>
            <ChartCard
              titleKey="warehouse.overview.categoryMix"
              subtitleKey="warehouse.overview.categoryMixSubtitle"
            >
              {categoryMix.length > 0 ? (
                <BarChart data={categoryMix} labelKey="warehouse.overview.categoryMix" />
              ) : (
                <EmptyState descriptionKey="warehouse.shared.emptyRows" />
              )}
            </ChartCard>

            <ChartCard
              titleKey="warehouse.overview.activityTimeline"
              subtitleKey="warehouse.overview.activitySubtitle"
            >
              {warehouseActivity.length > 0 ? (
                <DataTable
                  rows={warehouseActivity}
                  columns={activityColumns}
                  rowKey={(row) => row.id}
                  pageSize={5}
                  captionKey="warehouse.overview.activityTimeline"
                  emptyMessageKey="warehouse.overview.noActivity"
                />
              ) : (
                <EmptyState descriptionKey="warehouse.overview.noActivity" />
              )}
            </ChartCard>
          </div>

          <ChartCard
            titleKey="warehouse.overview.alertsPanel"
            subtitleKey="warehouse.overview.alertsSubtitle"
          >
            {warehouseAlerts.length > 0 ? (
              <DataTable
                rows={warehouseAlerts}
                columns={alertColumns}
                rowKey={(row) => row.id}
                pageSize={5}
                captionKey="warehouse.overview.alertsPanel"
                emptyMessageKey="warehouse.overview.noAlerts"
              />
            ) : (
              <EmptyState descriptionKey="warehouse.overview.noAlerts" />
            )}
          </ChartCard>
        </>
      )}
    </>
  );
}

export default OverviewPage;
