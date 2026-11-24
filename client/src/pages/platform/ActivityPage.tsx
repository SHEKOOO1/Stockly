import { Eye } from 'lucide-react';
import { useMemo, useState } from 'react';
import {
  DataTable,
  DemoNotice,
  ErrorState,
  LoadingState,
  Modal,
  PageHeader,
  SearchFilterBar,
  StatusBadge,
  type DataTableColumn,
} from '@/components/ui';
import { demoActivities, demoOrganizations, demoWarehouses } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatDateTime } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { Activity, AppContextScope, StatusTone } from '@/types/domain';

const filterSelectStyle = { minWidth: 150 } as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

const scopeOptions: readonly AppContextScope[] = ['platform', 'organization', 'warehouse'];
const toneOptions: readonly StatusTone[] = ['healthy', 'warning', 'critical', 'neutral', 'info'];

const scopeLabels: Record<AppContextScope, TranslationKey> = {
  platform: 'platform.activity.scopePlatform',
  organization: 'platform.activity.scopeOrganization',
  warehouse: 'platform.activity.scopeWarehouse',
};

const toneLabels: Record<StatusTone, TranslationKey> = {
  healthy: 'common.status.healthy',
  warning: 'common.status.warning',
  critical: 'common.status.critical',
  neutral: 'common.status.neutral',
  info: 'common.status.info',
};

function organizationName(id: string | null): string {
  return (
    demoOrganizations.find((organization) => organization.id === id)?.name ??
    t('common.states.notAvailable')
  );
}

function warehouseName(id: string | null): string {
  return (
    demoWarehouses.find((warehouse) => warehouse.id === id)?.name ?? t('common.states.notAvailable')
  );
}

export function PlatformActivityPage() {
  const loader = useDemoLoader(demoActivities);
  const [search, setSearch] = useState('');
  const [scopeFilter, setScopeFilter] = useState<AppContextScope | ''>('');
  const [toneFilter, setToneFilter] = useState<StatusTone | ''>('');
  const [selected, setSelected] = useState<Activity | null>(null);

  const filteredRows = useMemo(
    () =>
      loader.data.filter((item) => {
        const needle = search.trim().toLocaleLowerCase();
        const matchesSearch =
          needle === '' || `${item.action} ${item.actor}`.toLocaleLowerCase().includes(needle);
        const matchesScope = scopeFilter === '' || item.scope === scopeFilter;
        const matchesTone = toneFilter === '' || item.tone === toneFilter;
        return matchesSearch && matchesScope && matchesTone;
      }),
    [loader.data, search, scopeFilter, toneFilter],
  );

  const columns = useMemo<DataTableColumn<Activity>[]>(
    () => [
      {
        id: 'action',
        headerKey: 'platform.activity.actionColumn',
        cell: (row) => <strong>{row.action}</strong>,
        sortValue: (row) => row.action,
      },
      {
        id: 'scope',
        headerKey: 'platform.activity.scopeColumn',
        cell: (row) => t(scopeLabels[row.scope]),
        sortValue: (row) => t(scopeLabels[row.scope]),
      },
      {
        id: 'organization',
        headerKey: 'platform.activity.organizationColumn',
        cell: (row) => organizationName(row.organizationId),
      },
      {
        id: 'warehouse',
        headerKey: 'platform.activity.warehouseColumn',
        cell: (row) => warehouseName(row.warehouseId),
      },
      {
        id: 'actor',
        headerKey: 'platform.activity.actorColumn',
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
        headerKey: 'platform.activity.timeColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
        sortValue: (row) => row.occurredAt,
      },
      {
        id: 'details',
        headerKey: 'platform.activity.detailsColumn',
        cell: (row) => (
          <button
            type="button"
            className="button button--quiet button--xs"
            onClick={() => setSelected(row)}
            aria-label={t('platform.activity.detailsAction')}
          >
            <Eye size={14} aria-hidden="true" />
            {t('platform.activity.detailsAction')}
          </button>
        ),
      },
    ],
    [],
  );

  function resetFilters() {
    setSearch('');
    setScopeFilter('');
    setToneFilter('');
  }

  return (
    <>
      <PageHeader
        titleKey="platform.activity.title"
        descriptionKey="platform.activity.description"
        eyebrowKey="common.context.platform"
      />
      <DemoNotice compact />

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="platform.activity.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <>
              <select
                value={scopeFilter}
                onChange={(event) => setScopeFilter(event.target.value as AppContextScope | '')}
                aria-label={t('platform.activity.scopeFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {scopeOptions.map((scope) => (
                  <option key={scope} value={scope}>
                    {t(scopeLabels[scope])}
                  </option>
                ))}
              </select>
              <select
                value={toneFilter}
                onChange={(event) => setToneFilter(event.target.value as StatusTone | '')}
                aria-label={t('platform.activity.toneFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {toneOptions.map((tone) => (
                  <option key={tone} value={tone}>
                    {t(toneLabels[tone])}
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
          rowKey={(row) => row.id}
          pageSize={6}
          captionKey="platform.activity.title"
          emptyMessageKey="platform.activity.emptyRows"
        />
      )}

      <Modal
        open={selected !== null}
        onClose={() => setSelected(null)}
        titleKey="platform.activity.detailsTitle"
        descriptionKey="platform.activity.detailsDescription"
        footer={
          <button type="button" className="button button--quiet" onClick={() => setSelected(null)}>
            {t('common.actions.close')}
          </button>
        }
      >
        <div style={summaryStyle}>
          <div className="profile-detail">
            <span>{t('platform.activity.eventId')}</span>
            <strong dir="ltr">{selected?.id}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('platform.activity.actionColumn')}</span>
            <strong>{selected?.action}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('platform.activity.scopeColumn')}</span>
            <strong>{selected ? t(scopeLabels[selected.scope]) : ''}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('platform.activity.organizationColumn')}</span>
            <strong>{selected ? organizationName(selected.organizationId) : ''}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('platform.activity.warehouseColumn')}</span>
            <strong>{selected ? warehouseName(selected.warehouseId) : ''}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('platform.activity.actorColumn')}</span>
            <strong>{selected?.actor}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('platform.activity.toneColumn')}</span>
            {selected ? (
              <StatusBadge tone={selected.tone} labelKey={toneLabels[selected.tone]} />
            ) : null}
          </div>
          <div className="profile-detail">
            <span>{t('platform.activity.timeColumn')}</span>
            <strong dir="ltr">{selected ? formatDateTime(selected.occurredAt) : ''}</strong>
          </div>
        </div>
      </Modal>
    </>
  );
}

export default PlatformActivityPage;
