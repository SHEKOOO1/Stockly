import { AlertTriangle, Package, Users, Warehouse } from 'lucide-react';
import { useMemo } from 'react';
import { Link } from 'react-router-dom';
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
import { demoActivities, demoAlerts, demoPlans, demoUsers } from '@/data/demoData';
import { useDemoProductStore } from '@/data/demoProductStore';
import { t, type TranslationKey } from '@/i18n';
import { formatDateTime, formatNumber } from '@/lib/format';
import type { ProductSummary } from '@/lib/product';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type {
  Activity as ActivityRecord,
  ChartPoint,
  DemoAlert,
  ProductStockStatus,
  StatusTone,
} from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(210px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const panelsStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(330px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

const productStatusLabels: Record<ProductStockStatus, TranslationKey> = {
  healthy: 'common.status.healthy',
  low: 'common.status.low',
  out: 'common.status.out',
  expiring: 'common.status.expiring',
};

const productStatusTones: Record<ProductStockStatus, StatusTone> = {
  healthy: 'healthy',
  low: 'warning',
  out: 'critical',
  expiring: 'warning',
};

const toneLabels: Record<StatusTone, TranslationKey> = {
  healthy: 'common.status.healthy',
  warning: 'common.status.warning',
  critical: 'common.status.critical',
  neutral: 'common.status.neutral',
  info: 'common.status.info',
};

export function OrganizationDashboardPage() {
  const { availableWarehouses, selectedOrganization, selectedWarehouse } = useApp();
  const { productSummaries } = useDemoProductStore();
  const productLoader = useDemoLoader(productSummaries, { delayMs: 180 });
  const activityLoader = useDemoLoader(demoActivities, { delayMs: 210 });
  const alertLoader = useDemoLoader(demoAlerts, { delayMs: 240 });

  const warehouseIds = useMemo(
    () => new Set(availableWarehouses.map((warehouse) => warehouse.id)),
    [availableWarehouses],
  );

  const warehouseCodeById = useMemo(
    () => new Map(availableWarehouses.map((warehouse) => [warehouse.id, warehouse.code])),
    [availableWarehouses],
  );

  const scopedUsers = useMemo(
    () => demoUsers.filter((user) => user.organizationIds.includes(selectedOrganization.id)),
    [selectedOrganization.id],
  );

  const scopedProducts = useMemo<ProductSummary[]>(
    () => productLoader.data.filter((summary) => warehouseIds.has(summary.product.warehouseId)),
    [productLoader.data, warehouseIds],
  );

  const scopedActivities = useMemo(
    () =>
      activityLoader.data
        .filter((item) => item.organizationId === selectedOrganization.id)
        .sort((left, right) => right.occurredAt.localeCompare(left.occurredAt))
        .slice(0, 5),
    [activityLoader.data, selectedOrganization.id],
  );

  const scopedAlerts = useMemo(
    () =>
      alertLoader.data
        .filter((item) => warehouseIds.has(item.warehouseId) && item.status !== 'inactive')
        .sort((left, right) => right.detectedAt.localeCompare(left.detectedAt))
        .slice(0, 5),
    [alertLoader.data, warehouseIds],
  );

  const inventoryPoints = useMemo<ChartPoint[]>(
    () =>
      availableWarehouses.map((warehouse) => ({
        id: warehouse.id,
        label: warehouse.code,
        value: scopedProducts
          .filter((summary) => summary.product.warehouseId === warehouse.id)
          .reduce((total, summary) => total + summary.onHandQuantity, 0),
        tone: 'info',
      })),
    [availableWarehouses, scopedProducts],
  );

  const productColumns = useMemo<DataTableColumn<ProductSummary>[]>(
    () => [
      {
        id: 'product',
        headerKey: 'organization.dashboard.productColumn',
        cell: (row) => <strong>{row.product.name}</strong>,
        sortValue: (row) => row.product.name,
      },
      {
        id: 'warehouse',
        headerKey: 'organization.dashboard.warehouseColumn',
        cell: (row) => (
          <span dir="ltr">
            {warehouseCodeById.get(row.product.warehouseId) ?? t('common.states.notAvailable')}
          </span>
        ),
        ltr: true,
      },
      {
        id: 'status',
        headerKey: 'common.labels.status',
        cell: (row) => (
          <StatusBadge
            tone={productStatusTones[row.stockStatus]}
            labelKey={productStatusLabels[row.stockStatus]}
          />
        ),
      },
      {
        id: 'quantity',
        headerKey: 'common.labels.quantity',
        cell: (row) => <span dir="ltr">{formatNumber(row.onHandQuantity)}</span>,
        ltr: true,
        align: 'end',
        sortValue: (row) => row.onHandQuantity,
      },
      {
        id: 'minimum',
        headerKey: 'organization.dashboard.minimumColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.minimumQuantity)}</span>,
        ltr: true,
        align: 'end',
        sortValue: (row) => row.minimumQuantity,
      },
    ],
    [warehouseCodeById],
  );

  const activityColumns = useMemo<DataTableColumn<ActivityRecord>[]>(
    () => [
      {
        id: 'action',
        headerKey: 'organization.dashboard.actionColumn',
        cell: (row) => <strong>{row.action}</strong>,
        sortValue: (row) => row.action,
      },
      {
        id: 'actor',
        headerKey: 'organization.dashboard.actorColumn',
        cell: (row) => row.actor,
        sortValue: (row) => row.actor,
      },
      {
        id: 'severity',
        headerKey: 'organization.dashboard.severityColumn',
        cell: (row) => <StatusBadge tone={row.tone} labelKey={toneLabels[row.tone]} />,
      },
      {
        id: 'time',
        headerKey: 'organization.dashboard.timeColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
        ltr: true,
        sortValue: (row) => row.occurredAt,
      },
    ],
    [],
  );

  const alertColumns = useMemo<DataTableColumn<DemoAlert>[]>(
    () => [
      {
        id: 'title',
        headerKey: 'organization.dashboard.actionColumn',
        cell: (row) => <strong>{row.title}</strong>,
        sortValue: (row) => row.title,
      },
      {
        id: 'severity',
        headerKey: 'organization.dashboard.severityColumn',
        cell: (row) => <StatusBadge tone={row.severity} labelKey={toneLabels[row.severity]} />,
      },
      {
        id: 'warehouse',
        headerKey: 'organization.dashboard.warehouseColumn',
        cell: (row) => (
          <span dir="ltr">
            {warehouseCodeById.get(row.warehouseId) ?? t('common.states.notAvailable')}
          </span>
        ),
        ltr: true,
      },
      {
        id: 'product',
        headerKey: 'organization.dashboard.productColumn',
        cell: (row) =>
          productSummaries.find((summary) => summary.product.id === row.productId)?.product.name ??
          row.productId,
      },
      {
        id: 'time',
        headerKey: 'organization.dashboard.timeColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.detectedAt)}</span>,
        ltr: true,
        sortValue: (row) => row.detectedAt,
      },
    ],
    [productSummaries, warehouseCodeById],
  );

  const plan = demoPlans.find((item) => item.id === selectedOrganization.planId);

  return (
    <>
      <PageHeader
        titleKey="organization.dashboard.title"
        descriptionKey="organization.dashboard.description"
        eyebrowKey="common.context.organization"
      />
      <DemoNotice compact />

      <div style={statGridStyle}>
        <StatCard
          labelKey="organization.dashboard.warehouseCount"
          value={formatNumber(availableWarehouses.length)}
          hintKey="organization.dashboard.warehouseHint"
          icon={Warehouse}
          tone="info"
        />
        <StatCard
          labelKey="organization.dashboard.userCount"
          value={formatNumber(scopedUsers.length)}
          hintKey="organization.dashboard.userHint"
          icon={Users}
          tone="healthy"
        />
        <StatCard
          labelKey="organization.dashboard.productCount"
          value={formatNumber(scopedProducts.length)}
          hintKey="organization.dashboard.productHint"
          icon={Package}
          tone="neutral"
        />
        <StatCard
          labelKey="organization.dashboard.alertCount"
          value={formatNumber(scopedAlerts.length)}
          hintKey="organization.dashboard.alertHint"
          icon={AlertTriangle}
          tone="warning"
        />
      </div>

      <div style={panelsStyle}>
        <ChartCard
          titleKey="organization.dashboard.summaryTitle"
          subtitleKey="organization.dashboard.summarySubtitle"
        >
          <div style={summaryStyle}>
            <div className="profile-detail">
              <span>{t('organization.dashboard.organizationLabel')}</span>
              <strong>{selectedOrganization.name}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('organization.dashboard.codeLabel')}</span>
              <strong dir="ltr">{selectedOrganization.code}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('organization.dashboard.planLabel')}</span>
              <strong>{plan?.name ?? selectedOrganization.planId}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('organization.dashboard.createdLabel')}</span>
              <strong dir="ltr">{formatDateTime(selectedOrganization.createdAt)}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('common.context.warehouse')}</span>
              <strong>{selectedWarehouse.name}</strong>
            </div>
          </div>
        </ChartCard>

        <ChartCard
          titleKey="organization.dashboard.inventoryChartTitle"
          subtitleKey="organization.dashboard.inventoryChartSubtitle"
        >
          {productLoader.status === 'error' ? (
            <ErrorState onRetry={productLoader.reload} />
          ) : productLoader.status === 'loading' ? (
            <LoadingState />
          ) : inventoryPoints.length > 0 ? (
            <BarChart
              data={inventoryPoints}
              labelKey="organization.dashboard.inventoryChartTitle"
            />
          ) : (
            <EmptyState />
          )}
        </ChartCard>
      </div>

      <ChartCard
        titleKey="organization.dashboard.productTableTitle"
        subtitleKey="organization.dashboard.productTableSubtitle"
      >
        {productLoader.status === 'error' ? (
          <ErrorState onRetry={productLoader.reload} />
        ) : productLoader.status === 'loading' ? (
          <LoadingState />
        ) : (
          <DataTable
            rows={[...scopedProducts]
              .sort((left, right) => right.onHandQuantity - left.onHandQuantity)
              .slice(0, 6)}
            columns={productColumns}
            rowKey={(row) => row.product.id}
            pageSize={6}
            captionKey="organization.dashboard.productTableTitle"
            emptyMessageKey="organization.dashboard.noProducts"
          />
        )}
      </ChartCard>

      <div style={panelsStyle}>
        <ChartCard
          titleKey="organization.dashboard.activitiesTitle"
          subtitleKey="organization.dashboard.activitiesSubtitle"
          actions={
            <Link className="button button--quiet button--sm" to="/organization/activity">
              {t('common.actions.view')}
            </Link>
          }
        >
          {activityLoader.status === 'error' ? (
            <ErrorState onRetry={activityLoader.reload} />
          ) : activityLoader.status === 'loading' ? (
            <LoadingState />
          ) : (
            <DataTable
              rows={scopedActivities}
              columns={activityColumns}
              rowKey={(row) => row.id}
              pageSize={5}
              captionKey="organization.dashboard.activitiesTitle"
              emptyMessageKey="organization.dashboard.noActivities"
            />
          )}
        </ChartCard>

        <ChartCard
          titleKey="organization.dashboard.alertsTitle"
          subtitleKey="organization.dashboard.alertsSubtitle"
        >
          {alertLoader.status === 'error' ? (
            <ErrorState onRetry={alertLoader.reload} />
          ) : alertLoader.status === 'loading' ? (
            <LoadingState />
          ) : (
            <DataTable
              rows={scopedAlerts}
              columns={alertColumns}
              rowKey={(row) => row.id}
              pageSize={5}
              captionKey="organization.dashboard.alertsTitle"
              emptyMessageKey="organization.dashboard.noAlerts"
            />
          )}
        </ChartCard>
      </div>
    </>
  );
}

export default OrganizationDashboardPage;
