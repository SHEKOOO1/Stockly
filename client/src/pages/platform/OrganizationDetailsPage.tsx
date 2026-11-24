import { ArrowLeft, CreditCard, Shield, Users, Warehouse } from 'lucide-react';
import { useMemo } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  ChartCard,
  DataTable,
  DemoNotice,
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
  StatCard,
  StatusBadge,
  Tabs,
  type DataTableColumn,
  type TabItem,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoActivities, demoPlans, demoWarehouses } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatCurrency, formatDate, formatDateTime, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type {
  Activity,
  AppContextScope,
  EntityStatus,
  StatusTone,
  Warehouse as WarehouseRecord,
} from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
  gap: '16px',
} as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

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

const toneLabels: Record<StatusTone, TranslationKey> = {
  healthy: 'common.status.healthy',
  warning: 'common.status.warning',
  critical: 'common.status.critical',
  neutral: 'common.status.neutral',
  info: 'common.status.info',
};

const scopeLabels: Record<AppContextScope, TranslationKey> = {
  platform: 'common.context.platform',
  organization: 'common.context.organization',
  warehouse: 'common.context.warehouse',
};

export function OrganizationDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { organizations } = useApp();
  const warehouseLoader = useDemoLoader(demoWarehouses, { delayMs: 180 });
  const activityLoader = useDemoLoader(demoActivities, { delayMs: 240 });

  const organization = useMemo(
    () => organizations.find((item) => item.id === id),
    [organizations, id],
  );

  const warehouseRows = useMemo(
    () => warehouseLoader.data.filter((warehouse) => warehouse.organizationId === id),
    [warehouseLoader.data, id],
  );

  const activityRows = useMemo(
    () => activityLoader.data.filter((item) => item.organizationId === id),
    [activityLoader.data, id],
  );

  const warehouseColumns = useMemo<DataTableColumn<WarehouseRecord>[]>(
    () => [
      {
        id: 'name',
        headerKey: 'common.labels.name',
        cell: (row) => <strong>{row.name}</strong>,
        sortValue: (row) => row.name,
      },
      {
        id: 'code',
        headerKey: 'platform.organizationDetails.codeColumn',
        cell: (row) => <span dir="ltr">{row.code}</span>,
        ltr: true,
        sortValue: (row) => row.code,
      },
      {
        id: 'status',
        headerKey: 'platform.organizationDetails.statusColumn',
        cell: (row) => (
          <StatusBadge tone={statusTones[row.status]} labelKey={statusLabels[row.status]} />
        ),
      },
      {
        id: 'productCount',
        headerKey: 'platform.organizationDetails.productCountColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.productCount)}</span>,
        sortValue: (row) => row.productCount,
        align: 'end',
      },
      {
        id: 'stockValue',
        headerKey: 'platform.organizationDetails.stockValueColumn',
        cell: (row) => <span dir="ltr">{formatCurrency(row.stockValue, 'SAR')}</span>,
        sortValue: (row) => row.stockValue,
        align: 'end',
      },
      {
        id: 'lastActivityAt',
        headerKey: 'platform.organizationDetails.lastActivityColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.lastActivityAt)}</span>,
        sortValue: (row) => row.lastActivityAt,
      },
    ],
    [],
  );

  const activityColumns = useMemo<DataTableColumn<Activity>[]>(
    () => [
      {
        id: 'action',
        headerKey: 'platform.organizationDetails.actionColumn',
        cell: (row) => <strong>{row.action}</strong>,
        sortValue: (row) => row.action,
      },
      {
        id: 'scope',
        headerKey: 'platform.activity.scopeColumn',
        cell: (row) => t(scopeLabels[row.scope]),
      },
      {
        id: 'actor',
        headerKey: 'platform.organizationDetails.actorColumn',
        cell: (row) => row.actor,
        sortValue: (row) => row.actor,
      },
      {
        id: 'tone',
        headerKey: 'platform.activity.toneColumn',
        cell: (row) => <StatusBadge tone={row.tone} labelKey={toneLabels[row.tone]} />,
      },
      {
        id: 'occurredAt',
        headerKey: 'platform.organizationDetails.timeColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
        sortValue: (row) => row.occurredAt,
      },
    ],
    [],
  );

  if (!organization) {
    return (
      <>
        <PageHeader
          titleKey="platform.organizationDetails.title"
          descriptionKey="platform.organizationDetails.description"
          eyebrowKey="common.context.platform"
        />
        <DemoNotice compact />
        <EmptyState
          titleKey="platform.organizationDetails.notFoundTitle"
          descriptionKey="platform.organizationDetails.notFoundDescription"
          actionLabelKey="platform.organizationDetails.backToOrganizations"
          onAction={() => navigate('/platform/organizations')}
        />
      </>
    );
  }

  const plan = demoPlans.find((item) => item.id === organization.planId);

  const tabs: TabItem[] = [
    {
      id: 'overview',
      labelKey: 'platform.organizationDetails.overviewTab',
      content: (
        <ChartCard
          titleKey="platform.organizationDetails.overviewTitle"
          subtitleKey="platform.organizationDetails.overviewDescription"
        >
          <div style={summaryStyle}>
            <div className="profile-detail">
              <span>{t('common.labels.code')}</span>
              <strong dir="ltr">{organization.code}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('common.labels.status')}</span>
              <StatusBadge
                tone={statusTones[organization.status]}
                labelKey={statusLabels[organization.status]}
              />
            </div>
            <div className="profile-detail">
              <span>{t('platform.dashboard.planColumn')}</span>
              <strong>{plan?.name ?? organization.planId}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('platform.organizationDetails.usageLabel')}</span>
              <strong dir="ltr">
                {formatNumber(organization.warehouseCount)} / {formatNumber(organization.userCount)}
              </strong>
            </div>
            <div className="profile-detail">
              <span>{t('common.labels.createdAt')}</span>
              <strong dir="ltr">{formatDate(organization.createdAt)}</strong>
            </div>
          </div>
          <div style={{ marginBlockStart: '16px' }}>
            <Link
              className="button button--quiet button--sm"
              to={`/platform/organizations/${organization.id}/administrators`}
            >
              <Shield size={14} aria-hidden="true" />
              {t('platform.organizationDetails.manageAdmins')}
            </Link>
          </div>
        </ChartCard>
      ),
    },
    {
      id: 'plan',
      labelKey: 'platform.organizationDetails.planTab',
      content: (
        <>
          <div style={statGridStyle}>
            <StatCard
              labelKey="platform.organizationDetails.planName"
              value={plan?.name ?? organization.planId}
              icon={CreditCard}
              tone="info"
            />
            <StatCard
              labelKey="platform.organizationDetails.monthlyPrice"
              value={formatCurrency(plan?.price ?? 0, plan?.currency ?? 'SAR')}
              icon={CreditCard}
              tone="healthy"
            />
            <StatCard
              labelKey="platform.organizationDetails.usageLabel"
              value={formatNumber(organization.userCount)}
              hintKey="platform.organizations.usersColumn"
              icon={Users}
              tone="neutral"
            />
            <StatCard
              labelKey="platform.organizations.warehousesColumn"
              value={formatNumber(organization.warehouseCount)}
              icon={Warehouse}
              tone="warning"
            />
          </div>
          <ChartCard
            titleKey="platform.organizationDetails.planTitle"
            subtitleKey="platform.organizationDetails.planDescription"
          >
            <div style={summaryStyle}>
              <div className="profile-detail">
                <span>{t('platform.organizationDetails.userLimit')}</span>
                <strong dir="ltr">{formatNumber(plan?.userLimit ?? 0)}</strong>
              </div>
              <div className="profile-detail">
                <span>{t('platform.organizationDetails.warehouseLimit')}</span>
                <strong dir="ltr">{formatNumber(plan?.warehouseLimit ?? 0)}</strong>
              </div>
              <div className="profile-detail">
                <span>{t('common.labels.status')}</span>
                <StatusBadge
                  tone={plan?.status === 'active' ? 'healthy' : 'neutral'}
                  labelKey={
                    plan?.status === 'active' ? 'common.status.active' : 'common.status.inactive'
                  }
                />
              </div>
            </div>
          </ChartCard>
        </>
      ),
    },
    {
      id: 'warehouses',
      labelKey: 'platform.organizationDetails.warehousesTab',
      content: (
        <ChartCard
          titleKey="platform.organizationDetails.warehousesTitle"
          subtitleKey="platform.organizationDetails.warehousesDescription"
        >
          {warehouseLoader.status === 'error' ? (
            <ErrorState onRetry={warehouseLoader.reload} />
          ) : warehouseLoader.status === 'loading' ? (
            <LoadingState />
          ) : (
            <DataTable
              rows={warehouseRows}
              columns={warehouseColumns}
              rowKey={(row) => row.id}
              pageSize={5}
              captionKey="platform.organizationDetails.warehousesTitle"
              emptyMessageKey="platform.organizationDetails.emptyWarehouses"
            />
          )}
        </ChartCard>
      ),
    },
    {
      id: 'activity',
      labelKey: 'platform.organizationDetails.activityTab',
      content: (
        <ChartCard
          titleKey="platform.organizationDetails.activityTitle"
          subtitleKey="platform.organizationDetails.activityDescription"
        >
          {activityLoader.status === 'error' ? (
            <ErrorState onRetry={activityLoader.reload} />
          ) : activityLoader.status === 'loading' ? (
            <LoadingState />
          ) : (
            <DataTable
              rows={activityRows}
              columns={activityColumns}
              rowKey={(row) => row.id}
              pageSize={5}
              captionKey="platform.organizationDetails.activityTitle"
              emptyMessageKey="platform.organizationDetails.emptyActivity"
            />
          )}
        </ChartCard>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        titleKey="platform.organizationDetails.title"
        descriptionKey="platform.organizationDetails.description"
        eyebrowKey="common.context.organization"
        actions={
          <Link className="button button--quiet button--sm" to="/platform/organizations">
            <ArrowLeft size={14} aria-hidden="true" />
            {t('platform.organizationDetails.backToOrganizations')}
          </Link>
        }
      />
      <DemoNotice compact />
      <Tabs items={tabs} defaultValue="overview" ariaLabelKey="common.labels.details" />
    </>
  );
}

export default OrganizationDetailsPage;
