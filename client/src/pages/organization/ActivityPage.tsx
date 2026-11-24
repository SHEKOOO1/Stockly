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
import { useApp } from '@/context/AppContext';
import { demoActivities } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatDateTime } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { Activity, AppContextScope, StatusTone } from '@/types/domain';

const filterSelectStyle = { minWidth: 150 } as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

const scopeOptions: readonly AppContextScope[] = ['organization', 'warehouse'];
const toneOptions: readonly StatusTone[] = ['healthy', 'warning', 'critical', 'neutral', 'info'];

const scopeLabels: Record<AppContextScope, TranslationKey> = {
  platform: 'common.context.platform',
  organization: 'organization.activity.scopeOrganization',
  warehouse: 'organization.activity.scopeWarehouse',
};

const toneLabels: Record<StatusTone, TranslationKey> = {
  healthy: 'common.status.healthy',
  warning: 'common.status.warning',
  critical: 'common.status.critical',
  neutral: 'common.status.neutral',
  info: 'common.status.info',
};

export function OrganizationActivityPage() {
  const { availableWarehouses, selectedOrganization } = useApp();
  const loader = useDemoLoader(demoActivities, { delayMs: 220 });
  const [search, setSearch] = useState('');
  const [scopeFilter, setScopeFilter] = useState<AppContextScope | ''>('');
  const [warehouseFilter, setWarehouseFilter] = useState('');
  const [toneFilter, setToneFilter] = useState<StatusTone | ''>('');
  const [selected, setSelected] = useState<Activity | null>(null);

  const warehouseNameById = useMemo(
    () => new Map(availableWarehouses.map((warehouse) => [warehouse.id, warehouse.name])),
    [availableWarehouses],
  );

  const scopedActivities = useMemo(
    () => loader.data.filter((item) => item.organizationId === selectedOrganization.id),
    [loader.data, selectedOrganization.id],
  );

  const filteredRows = useMemo(
    () =>
      scopedActivities.filter((item) => {
        const needle = search.trim().toLocaleLowerCase();
        const matchesSearch =
          needle === '' || `${item.action} ${item.actor}`.toLocaleLowerCase().includes(needle);
        const matchesScope = scopeFilter === '' || item.scope === scopeFilter;
        const matchesWarehouse = warehouseFilter === '' || item.warehouseId === warehouseFilter;
        const matchesTone = toneFilter === '' || item.tone === toneFilter;
        return matchesSearch && matchesScope && matchesWarehouse && matchesTone;
      }),
    [scopedActivities, search, scopeFilter, warehouseFilter, toneFilter],
  );

  const columns = useMemo<DataTableColumn<Activity>[]>(
    () => [
      {
        id: 'action',
        headerKey: 'organization.activity.actionColumn',
        cell: (row) => <strong>{row.action}</strong>,
        sortValue: (row) => row.action,
      },
      {
        id: 'scope',
        headerKey: 'organization.activity.scopeColumn',
        cell: (row) => t(scopeLabels[row.scope]),
        sortValue: (row) => t(scopeLabels[row.scope]),
      },
      {
        id: 'warehouse',
        headerKey: 'organization.activity.warehouseColumn',
        cell: (row) =>
          row.warehouseId
            ? (warehouseNameById.get(row.warehouseId) ?? t('common.states.notAvailable'))
            : t('common.states.notAvailable'),
      },
      {
        id: 'actor',
        headerKey: 'organization.activity.actorColumn',
        cell: (row) => row.actor,
        sortValue: (row) => row.actor,
      },
      {
        id: 'tone',
        headerKey: 'organization.activity.toneColumn',
        cell: (row) => <StatusBadge tone={row.tone} labelKey={toneLabels[row.tone]} />,
      },
      {
        id: 'time',
        headerKey: 'organization.activity.timeColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
        ltr: true,
        sortValue: (row) => row.occurredAt,
      },
      {
        id: 'details',
        headerKey: 'organization.activity.detailsColumn',
        cell: (row) => (
          <button
            type="button"
            className="button button--quiet button--xs"
            onClick={() => setSelected(row)}
            aria-label={t('organization.activity.detailsAction')}
          >
            <Eye size={14} aria-hidden="true" />
            {t('organization.activity.detailsAction')}
          </button>
        ),
      },
    ],
    [warehouseNameById],
  );

  function resetFilters() {
    setSearch('');
    setScopeFilter('');
    setWarehouseFilter('');
    setToneFilter('');
  }

  return (
    <>
      <PageHeader
        titleKey="organization.activity.title"
        descriptionKey="organization.activity.description"
        eyebrowKey="common.context.organization"
      />
      <DemoNotice compact />

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
              placeholderKey="organization.activity.searchPlaceholder"
              resultCount={filteredRows.length}
              onReset={resetFilters}
              filters={
                <>
                  <select
                    value={scopeFilter}
                    onChange={(event) => setScopeFilter(event.target.value as AppContextScope | '')}
                    aria-label={t('organization.activity.scopeFilter')}
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
                    value={warehouseFilter}
                    onChange={(event) => setWarehouseFilter(event.target.value)}
                    aria-label={t('organization.activity.warehouseFilter')}
                    style={filterSelectStyle}
                  >
                    <option value="">{t('common.labels.all')}</option>
                    {availableWarehouses.map((warehouse) => (
                      <option key={warehouse.id} value={warehouse.id}>
                        {warehouse.name}
                      </option>
                    ))}
                  </select>
                  <select
                    value={toneFilter}
                    onChange={(event) => setToneFilter(event.target.value as StatusTone | '')}
                    aria-label={t('organization.activity.toneFilter')}
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

          <DataTable
            rows={filteredRows}
            columns={columns}
            rowKey={(row) => row.id}
            pageSize={7}
            captionKey="organization.activity.title"
            emptyMessageKey="organization.activity.emptyRows"
          />
        </>
      )}

      <Modal
        open={selected !== null}
        onClose={() => setSelected(null)}
        titleKey="organization.activity.detailsTitle"
        descriptionKey="organization.activity.detailsDescription"
        footer={
          <button type="button" className="button button--quiet" onClick={() => setSelected(null)}>
            {t('common.actions.close')}
          </button>
        }
      >
        <div style={summaryStyle}>
          <div className="profile-detail">
            <span>{t('organization.activity.eventId')}</span>
            <strong dir="ltr">{selected?.id}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.activity.actionColumn')}</span>
            <strong>{selected?.action}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.activity.scopeColumn')}</span>
            <strong>{selected ? t(scopeLabels[selected.scope]) : ''}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.activity.organizationColumn')}</span>
            <strong>{selectedOrganization.name}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.activity.warehouseColumn')}</span>
            <strong>
              {selected?.warehouseId
                ? (warehouseNameById.get(selected.warehouseId) ?? t('common.states.notAvailable'))
                : t('common.states.notAvailable')}
            </strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.activity.actorColumn')}</span>
            <strong>{selected?.actor}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.activity.toneColumn')}</span>
            {selected ? (
              <StatusBadge tone={selected.tone} labelKey={toneLabels[selected.tone]} />
            ) : null}
          </div>
          <div className="profile-detail">
            <span>{t('organization.activity.timeColumn')}</span>
            <strong dir="ltr">{selected ? formatDateTime(selected.occurredAt) : ''}</strong>
          </div>
        </div>
      </Modal>
    </>
  );
}

export default OrganizationActivityPage;
