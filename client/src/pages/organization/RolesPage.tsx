import { Eye, ShieldCheck } from 'lucide-react';
import { useMemo, useState } from 'react';
import {
  ChartCard,
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
import { demoRoles } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { EntityStatus, Role, StatusTone } from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(210px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

const catalogPermissionCount = 105;
const catalogNamespaceCount = 13;

const statusOptions: readonly EntityStatus[] = ['active', 'inactive'];

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

export function RolesPermissionsPage() {
  const { selectedOrganization } = useApp();
  const loader = useDemoLoader(demoRoles, { delayMs: 200 });
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [selected, setSelected] = useState<Role | null>(null);

  const scopedRoles = useMemo(
    () => demoRoles.filter((role) => role.organizationId === selectedOrganization.id),
    [selectedOrganization.id],
  );

  const filteredRoles = useMemo(
    () =>
      scopedRoles.filter((role) => {
        const needle = search.trim().toLocaleLowerCase();
        const matchesSearch =
          needle === '' || `${role.name} ${role.code}`.toLocaleLowerCase().includes(needle);
        const matchesStatus = statusFilter === '' || role.status === statusFilter;
        return matchesSearch && matchesStatus;
      }),
    [scopedRoles, search, statusFilter],
  );

  const columns = useMemo<DataTableColumn<Role>[]>(
    () => [
      {
        id: 'name',
        headerKey: 'organization.roles.nameColumn',
        cell: (row) => <strong>{row.name}</strong>,
        sortValue: (row) => row.name,
      },
      {
        id: 'code',
        headerKey: 'organization.roles.codeColumn',
        cell: (row) => <span dir="ltr">{row.code}</span>,
        ltr: true,
        sortValue: (row) => row.code,
      },
      {
        id: 'permissionCount',
        headerKey: 'organization.roles.permissionCountColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.permissionCount)}</span>,
        ltr: true,
        align: 'end',
        sortValue: (row) => row.permissionCount,
      },
      {
        id: 'status',
        headerKey: 'organization.roles.statusColumn',
        cell: (row) => (
          <StatusBadge tone={statusTones[row.status]} labelKey={statusLabels[row.status]} />
        ),
      },
      {
        id: 'details',
        headerKey: 'common.labels.details',
        cell: (row) => (
          <button
            type="button"
            className="button button--quiet button--xs"
            onClick={() => setSelected(row)}
            aria-label={t('organization.roles.detailsAction')}
          >
            <Eye size={14} aria-hidden="true" />
            {t('organization.roles.detailsAction')}
          </button>
        ),
      },
    ],
    [],
  );

  function resetFilters() {
    setSearch('');
    setStatusFilter('');
  }

  return (
    <>
      <PageHeader
        titleKey="organization.roles.title"
        descriptionKey="organization.roles.description"
        eyebrowKey="common.context.organization"
        actions={<StatusBadge tone="info" labelKey="common.demo.previewOnly" />}
      />
      <DemoNotice compact />

      <div style={statGridStyle}>
        <StatCard
          labelKey="organization.roles.permissionsCount"
          value={formatNumber(catalogPermissionCount)}
          hintKey="organization.roles.permissionsHint"
          icon={ShieldCheck}
          tone="info"
        />
        <StatCard
          labelKey="organization.roles.namespacesCount"
          value={formatNumber(catalogNamespaceCount)}
          hintKey="organization.roles.namespacesHint"
          icon={ShieldCheck}
          tone="healthy"
        />
        <StatCard
          labelKey="organization.roles.rolesTitle"
          value={formatNumber(scopedRoles.length)}
          hintKey="organization.roles.rolesSubtitle"
          icon={ShieldCheck}
          tone="neutral"
        />
      </div>

      <div style={{ marginBlockEnd: '20px' }}>
        <ChartCard
          titleKey="organization.roles.catalogTitle"
          subtitleKey="organization.roles.catalogSubtitle"
        >
          <div style={summaryStyle}>
            <div className="profile-detail">
              <span>{t('organization.roles.permissionsCount')}</span>
              <strong dir="ltr">{formatNumber(catalogPermissionCount)}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('organization.roles.namespacesCount')}</span>
              <strong dir="ltr">{formatNumber(catalogNamespaceCount)}</strong>
            </div>
            <div className="profile-detail">
              <span>{t('organization.roles.serverNoteTitle')}</span>
              <strong>{t('common.demo.previewOnly')}</strong>
            </div>
          </div>
        </ChartCard>
      </div>

      <ChartCard
        titleKey="organization.roles.rolesTitle"
        subtitleKey="organization.roles.rolesSubtitle"
      >
        {loader.status === 'error' ? (
          <ErrorState onRetry={loader.reload} />
        ) : loader.status === 'loading' ? (
          <LoadingState />
        ) : (
          <>
            <div style={{ marginBlockEnd: '16px' }}>
              <SearchFilterBar
                value={search}
                onChange={setSearch}
                placeholderKey="organization.roles.searchPlaceholder"
                resultCount={filteredRoles.length}
                onReset={resetFilters}
                filters={
                  <select
                    value={statusFilter}
                    onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
                    aria-label={t('organization.roles.statusFilter')}
                    style={{ minWidth: 150 }}
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
            <DataTable
              rows={filteredRoles}
              columns={columns}
              rowKey={(row) => row.id}
              pageSize={6}
              captionKey="organization.roles.rolesTitle"
              emptyMessageKey="organization.roles.emptyRows"
            />
          </>
        )}
      </ChartCard>

      <Modal
        open={selected !== null}
        onClose={() => setSelected(null)}
        titleKey="organization.roles.detailsTitle"
        descriptionKey="organization.roles.detailsDescription"
        footer={
          <button type="button" className="button button--quiet" onClick={() => setSelected(null)}>
            {t('common.actions.close')}
          </button>
        }
      >
        <div style={summaryStyle}>
          <div className="profile-detail">
            <span>{t('common.labels.name')}</span>
            <strong>{selected?.name}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.roles.codeColumn')}</span>
            <strong dir="ltr">{selected?.code}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.roles.permissionCountLabel')}</span>
            <strong dir="ltr">{selected ? formatNumber(selected.permissionCount) : ''}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('common.labels.status')}</span>
            {selected ? (
              <StatusBadge
                tone={statusTones[selected.status]}
                labelKey={statusLabels[selected.status]}
              />
            ) : null}
          </div>
        </div>
        <div style={{ marginBlockStart: '16px' }}>
          <strong>{t('organization.roles.serverNoteTitle')}</strong>
          <p className="form-field__hint">{t('organization.roles.serverNote')}</p>
        </div>
      </Modal>
    </>
  );
}

export default RolesPermissionsPage;
