import { Eye, Plus } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  ChartCard,
  ConfirmDialog,
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
import { t, type TranslationKey } from '@/i18n';
import { formatCurrency, formatDateTime, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { EntityStatus, StatusTone, Warehouse } from '@/types/domain';

const filterSelectStyle = { minWidth: 150 } as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

const actionsStyle = {
  display: 'flex',
  gap: '8px',
  flexWrap: 'wrap',
} as const;

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

export function WarehousesPage() {
  const {
    availableWarehouses,
    selectedOrganization,
    selectedWarehouseId,
    selectWarehouse,
    showToast,
  } = useApp();
  const loader = useDemoLoader(availableWarehouses, { delayMs: 200 });
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [statusOverrides, setStatusOverrides] = useState<Record<string, EntityStatus>>({});
  const [statusTarget, setStatusTarget] = useState<Warehouse | null>(null);
  const [selected, setSelected] = useState<Warehouse | null>(null);

  const rows = useMemo<Warehouse[]>(
    () =>
      loader.data.map((warehouse) => ({
        ...warehouse,
        status: statusOverrides[warehouse.id] ?? warehouse.status,
      })),
    [loader.data, statusOverrides],
  );

  const filteredRows = useMemo(
    () =>
      rows.filter((warehouse) => {
        const needle = search.trim().toLocaleLowerCase();
        const matchesSearch =
          needle === '' ||
          `${warehouse.name} ${warehouse.code}`.toLocaleLowerCase().includes(needle);
        const matchesStatus = statusFilter === '' || warehouse.status === statusFilter;
        return matchesSearch && matchesStatus;
      }),
    [rows, search, statusFilter],
  );

  const columns = useMemo<DataTableColumn<Warehouse>[]>(
    () => [
      {
        id: 'name',
        headerKey: 'organization.warehouses.nameColumn',
        cell: (row) => (
          <div>
            <strong>{row.name}</strong>
            {row.id === selectedWarehouseId ? (
              <div>
                <StatusBadge
                  tone="info"
                  labelKey="organization.warehouses.currentDemoLabel"
                  dot={false}
                />
              </div>
            ) : null}
          </div>
        ),
        sortValue: (row) => row.name,
      },
      {
        id: 'code',
        headerKey: 'organization.warehouses.codeColumn',
        cell: (row) => <span dir="ltr">{row.code}</span>,
        ltr: true,
        sortValue: (row) => row.code,
      },
      {
        id: 'status',
        headerKey: 'organization.warehouses.statusColumn',
        cell: (row) => (
          <StatusBadge tone={statusTones[row.status]} labelKey={statusLabels[row.status]} />
        ),
      },
      {
        id: 'products',
        headerKey: 'organization.warehouses.productCountColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.productCount)}</span>,
        ltr: true,
        align: 'end',
        sortValue: (row) => row.productCount,
      },
      {
        id: 'stockValue',
        headerKey: 'organization.warehouses.stockValueColumn',
        cell: (row) => <span dir="ltr">{formatCurrency(row.stockValue)}</span>,
        ltr: true,
        align: 'end',
        sortValue: (row) => row.stockValue,
      },
      {
        id: 'lastActivity',
        headerKey: 'organization.warehouses.lastActivityColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.lastActivityAt)}</span>,
        ltr: true,
        sortValue: (row) => row.lastActivityAt,
      },
      {
        id: 'actions',
        headerKey: 'organization.warehouses.actionsColumn',
        cell: (row) => (
          <div style={actionsStyle}>
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => setSelected(row)}
              aria-label={t('organization.warehouses.detailsAction')}
            >
              <Eye size={14} aria-hidden="true" />
              {t('organization.warehouses.detailsAction')}
            </button>
            <button
              type="button"
              className="button button--quiet button--xs"
              disabled={row.id === selectedWarehouseId}
              onClick={() => {
                selectWarehouse(row.id);
                showToast('organization.warehouses.setDemoToast', 'info');
              }}
            >
              {t('organization.warehouses.setDemoAction')}
            </button>
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => setStatusTarget(row)}
            >
              {t(
                row.status === 'active'
                  ? 'organization.warehouses.deactivateAction'
                  : 'organization.warehouses.activateAction',
              )}
            </button>
          </div>
        ),
      },
    ],
    [selectWarehouse, selectedWarehouseId, showToast],
  );

  function resetFilters() {
    setSearch('');
    setStatusFilter('');
  }

  function confirmStatusChange() {
    if (!statusTarget) {
      return;
    }
    const nextStatus: EntityStatus = statusTarget.status === 'active' ? 'inactive' : 'active';
    setStatusOverrides((current) => ({ ...current, [statusTarget.id]: nextStatus }));
    showToast('organization.warehouses.statusToast', 'info');
    setStatusTarget(null);
  }

  return (
    <>
      <PageHeader
        titleKey="organization.warehouses.title"
        descriptionKey="organization.warehouses.description"
        eyebrowKey="common.context.organization"
        actions={
          <Link className="button button--primary button--sm" to="/organization/warehouses/new">
            <Plus size={14} aria-hidden="true" />
            {t('organization.warehouses.newWarehouse')}
          </Link>
        }
      />
      <DemoNotice compact />

      <ChartCard
        titleKey="organization.warehouses.title"
        subtitleKey="organization.warehouses.tableSubtitle"
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
                placeholderKey="organization.warehouses.searchPlaceholder"
                resultCount={filteredRows.length}
                onReset={resetFilters}
                filters={
                  <select
                    value={statusFilter}
                    onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
                    aria-label={t('organization.warehouses.statusFilter')}
                    style={filterSelectStyle}
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
              rows={filteredRows}
              columns={columns}
              rowKey={(row) => row.id}
              pageSize={6}
              captionKey="organization.warehouses.title"
              emptyMessageKey="organization.warehouses.emptyRows"
            />
          </>
        )}
      </ChartCard>

      <Modal
        open={selected !== null}
        onClose={() => setSelected(null)}
        titleKey="organization.warehouses.detailsTitle"
        descriptionKey="organization.warehouses.detailsDescription"
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
            <span>{t('common.labels.code')}</span>
            <strong dir="ltr">{selected?.code}</strong>
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
          <div className="profile-detail">
            <span>{t('organization.warehouses.productCountColumn')}</span>
            <strong dir="ltr">{selected ? formatNumber(selected.productCount) : ''}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.warehouses.stockValueColumn')}</span>
            <strong dir="ltr">{selected ? formatCurrency(selected.stockValue) : ''}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.warehouses.lastActivityColumn')}</span>
            <strong dir="ltr">{selected ? formatDateTime(selected.lastActivityAt) : ''}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('common.context.organization')}</span>
            <strong>{selectedOrganization.name}</strong>
          </div>
        </div>
      </Modal>

      <ConfirmDialog
        open={statusTarget !== null}
        onClose={() => setStatusTarget(null)}
        onConfirm={confirmStatusChange}
        titleKey="organization.warehouses.statusConfirmTitle"
        messageKey={
          statusTarget?.status === 'active'
            ? 'organization.warehouses.statusDeactivateMessage'
            : 'organization.warehouses.statusActivateMessage'
        }
      />
    </>
  );
}

export default WarehousesPage;
