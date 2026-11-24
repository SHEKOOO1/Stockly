import { Activity as ActivityIcon, Building2, Users, Warehouse } from 'lucide-react';
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
import { demoActivities, demoOrganizations, demoPlans, demoUsers } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatDate, formatDateTime, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type {
  Activity as ActivityRecord,
  ChartPoint,
  EntityStatus,
  Organization,
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

export function PlatformDashboardPage() {
  const { organizations, warehouses } = useApp();
  const organizationsLoader = useDemoLoader(demoOrganizations, { delayMs: 180 });
  const activityLoader = useDemoLoader(demoActivities, { delayMs: 240 });

  const recentOrganizations = useMemo(
    () =>
      [...organizations]
        .sort((left, right) => right.createdAt.localeCompare(left.createdAt))
        .slice(0, 4),
    [organizations],
  );

  const recentEvents = useMemo(
    () =>
      [...demoActivities]
        .sort((left, right) => right.occurredAt.localeCompare(left.occurredAt))
        .slice(0, 5),
    [],
  );

  const activityByDay = useMemo<ChartPoint[]>(() => {
    const counts = new Map<string, number>();
    for (const item of demoActivities) {
      const day = item.occurredAt.slice(0, 10);
      counts.set(day, (counts.get(day) ?? 0) + 1);
    }
    return [...counts.entries()]
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([day, value]) => ({
        id: `activity-${day}`,
        label: formatDate(day, { day: 'numeric', month: 'short' }),
        value,
        tone: 'info',
      }));
  }, []);

  const organizationColumns = useMemo<DataTableColumn<Organization>[]>(
    () => [
      {
        id: 'name',
        headerKey: 'common.labels.name',
        cell: (row) => <strong>{row.name}</strong>,
        sortValue: (row) => row.name,
      },
      {
        id: 'code',
        headerKey: 'common.labels.code',
        cell: (row) => <span dir="ltr">{row.code}</span>,
        ltr: true,
        sortValue: (row) => row.code,
      },
      {
        id: 'plan',
        headerKey: 'platform.dashboard.planColumn',
        cell: (row) => demoPlans.find((plan) => plan.id === row.planId)?.name ?? row.planId,
      },
      {
        id: 'status',
        headerKey: 'common.labels.status',
        cell: (row) => (
          <StatusBadge tone={statusTones[row.status]} labelKey={statusLabels[row.status]} />
        ),
      },
      {
        id: 'createdAt',
        headerKey: 'common.labels.createdAt',
        cell: (row) => <span dir="ltr">{formatDate(row.createdAt)}</span>,
        sortValue: (row) => row.createdAt,
      },
    ],
    [],
  );

  const activityColumns = useMemo<DataTableColumn<ActivityRecord>[]>(
    () => [
      {
        id: 'action',
        headerKey: 'platform.dashboard.actionColumn',
        cell: (row) => <strong>{row.action}</strong>,
        sortValue: (row) => row.action,
      },
      {
        id: 'actor',
        headerKey: 'platform.dashboard.actorColumn',
        cell: (row) => row.actor,
        sortValue: (row) => row.actor,
      },
      {
        id: 'tone',
        headerKey: 'common.labels.status',
        cell: (row) => <StatusBadge tone={row.tone} labelKey={toneLabels[row.tone]} />,
      },
      {
        id: 'occurredAt',
        headerKey: 'platform.dashboard.timeColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
        sortValue: (row) => row.occurredAt,
      },
    ],
    [],
  );

  return (
    <>
      <PageHeader
        titleKey="platform.dashboard.title"
        descriptionKey="platform.dashboard.description"
        eyebrowKey="common.context.platform"
        actions={
          <Link className="button button--quiet button--sm" to="/platform/organizations">
            {t('platform.dashboard.viewAllOrganizations')}
          </Link>
        }
      />
      <DemoNotice compact />

      <div style={statGridStyle}>
        <StatCard
          labelKey="platform.dashboard.totalOrganizations"
          value={formatNumber(organizations.length)}
          hintKey="platform.dashboard.organizationsHint"
          icon={Building2}
          tone="info"
          trend={[1, 1, 2, 2, 3]}
        />
        <StatCard
          labelKey="platform.dashboard.totalWarehouses"
          value={formatNumber(warehouses.length)}
          hintKey="platform.dashboard.warehousesHint"
          icon={Warehouse}
          tone="healthy"
          trend={[2, 3, 3, 4, 6]}
        />
        <StatCard
          labelKey="platform.dashboard.totalUsers"
          value={formatNumber(demoUsers.length)}
          hintKey="platform.dashboard.usersHint"
          icon={Users}
          tone="neutral"
          trend={[3, 4, 4, 5, 7]}
        />
        <StatCard
          labelKey="platform.dashboard.platformActivity"
          value={formatNumber(demoActivities.length)}
          hintKey="platform.dashboard.activityHint"
          icon={ActivityIcon}
          tone="warning"
          trend={[1, 2, 1, 3, 2]}
        />
      </div>

      <div style={panelsStyle}>
        <ChartCard
          titleKey="platform.dashboard.activityChart"
          subtitleKey="platform.dashboard.activityChartSubtitle"
        >
          {activityByDay.length > 0 ? (
            <BarChart data={activityByDay} labelKey="platform.dashboard.activityChart" />
          ) : (
            <EmptyState />
          )}
        </ChartCard>

        <ChartCard
          titleKey="platform.dashboard.recentOrganizations"
          subtitleKey="platform.dashboard.recentOrganizationsSubtitle"
        >
          {organizationsLoader.status === 'error' ? (
            <ErrorState onRetry={organizationsLoader.reload} />
          ) : organizationsLoader.status === 'loading' ? (
            <LoadingState />
          ) : (
            <DataTable
              rows={recentOrganizations}
              columns={organizationColumns}
              rowKey={(row) => row.id}
              pageSize={4}
              captionKey="platform.dashboard.recentOrganizations"
              emptyMessageKey="platform.dashboard.noRecentOrganizations"
            />
          )}
        </ChartCard>
      </div>

      <ChartCard
        titleKey="platform.dashboard.recentEvents"
        subtitleKey="platform.dashboard.recentEventsSubtitle"
        actions={
          <Link className="button button--quiet button--sm" to="/platform/activity">
            {t('platform.dashboard.viewAllActivity')}
          </Link>
        }
      >
        {activityLoader.status === 'error' ? (
          <ErrorState onRetry={activityLoader.reload} />
        ) : activityLoader.status === 'loading' ? (
          <LoadingState />
        ) : (
          <DataTable
            rows={recentEvents}
            columns={activityColumns}
            rowKey={(row) => row.id}
            pageSize={5}
            captionKey="platform.dashboard.recentEvents"
            emptyMessageKey="platform.dashboard.noRecentEvents"
          />
        )}
      </ChartCard>
    </>
  );
}

export default PlatformDashboardPage;
