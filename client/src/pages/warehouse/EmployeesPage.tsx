import { Eye, Mail, Users } from 'lucide-react';
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
import { demoUsers } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { DemoUser, EntityStatus, StatusTone } from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const detailGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
  gap: '16px',
} as const;

const actionRowStyle = {
  display: 'flex',
  justifyContent: 'flex-end',
} as const;

const filterSelectStyle = { minWidth: 160 } as const;

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

export function WarehouseEmployeesPage() {
  const { selectedWarehouse } = useApp();
  const loader = useDemoLoader(demoUsers);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [details, setDetails] = useState<DemoUser | null>(null);

  const rows = useMemo(
    () => demoUsers.filter((user) => user.warehouseIds.includes(selectedWarehouse.id)),
    [selectedWarehouse.id],
  );

  const roles = useMemo(() => Array.from(new Set(rows.map((user) => user.role))), [rows]);

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return rows.filter((user) => {
      const matchesSearch =
        needle === '' || `${user.name} ${user.email}`.toLocaleLowerCase().includes(needle);
      const matchesRole = roleFilter === '' || user.role === roleFilter;
      const matchesStatus = statusFilter === '' || user.status === statusFilter;
      return matchesSearch && matchesRole && matchesStatus;
    });
  }, [rows, search, roleFilter, statusFilter]);

  const activeCount = useMemo(() => rows.filter((user) => user.status === 'active').length, [rows]);

  const columns: DataTableColumn<DemoUser>[] = [
    {
      id: 'name',
      headerKey: 'common.labels.name',
      cell: (row) => <strong>{row.name}</strong>,
      sortValue: (row) => row.name,
    },
    {
      id: 'role',
      headerKey: 'warehouse.employees.roleColumn',
      cell: (row) => row.role,
      sortValue: (row) => row.role,
    },
    {
      id: 'email',
      headerKey: 'warehouse.employees.emailColumn',
      cell: (row) => <span dir="ltr">{row.email}</span>,
      sortValue: (row) => row.email,
      ltr: true,
    },
    {
      id: 'warehouses',
      headerKey: 'warehouse.employees.warehousesColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.warehouseIds.length)}</span>,
      sortValue: (row) => row.warehouseIds.length,
      align: 'end',
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'warehouse.employees.statusColumn',
      cell: (row) => (
        <StatusBadge tone={statusTones[row.status]} labelKey={statusLabels[row.status]} />
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
    setRoleFilter('');
    setStatusFilter('');
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.employees.title"
        descriptionKey="warehouse.employees.description"
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
          labelKey="warehouse.employees.title"
          value={formatNumber(rows.length)}
          icon={Users}
          tone="info"
        />
        <StatCard
          labelKey="common.status.active"
          value={formatNumber(activeCount)}
          icon={Mail}
          tone="healthy"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.employees.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <>
              <select
                value={roleFilter}
                onChange={(event) => setRoleFilter(event.target.value)}
                aria-label={t('warehouse.employees.roleFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {roles.map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
              <select
                value={statusFilter}
                onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
                aria-label={t('warehouse.employees.statusFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {statusOptions.map((status) => (
                  <option key={status} value={status}>
                    {t(statusLabels[status])}
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
      ) : rows.length === 0 ? (
        <EmptyState descriptionKey="warehouse.employees.noEmployees" />
      ) : (
        <DataTable
          rows={filteredRows}
          columns={columns}
          rowKey={(row) => row.id}
          pageSize={7}
          captionKey="warehouse.employees.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={details !== null}
        onClose={() => setDetails(null)}
        titleKey="warehouse.employees.detailsTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={detailGridStyle}>
            <div>
              <strong>{t('common.labels.name')}</strong>
              <p>{details.name}</p>
            </div>
            <div>
              <strong>{t('warehouse.employees.roleColumn')}</strong>
              <p>{details.role}</p>
            </div>
            <div>
              <strong>{t('warehouse.employees.emailColumn')}</strong>
              <p dir="ltr">{details.email}</p>
            </div>
            <div>
              <strong>{t('warehouse.employees.warehousesColumn')}</strong>
              <p dir="ltr">{formatNumber(details.warehouseIds.length)}</p>
            </div>
            <div>
              <strong>{t('common.labels.status')}</strong>
              <p>
                <StatusBadge
                  tone={statusTones[details.status]}
                  labelKey={statusLabels[details.status]}
                />
              </p>
            </div>
          </div>
        ) : null}
      </Modal>
    </>
  );
}

export default WarehouseEmployeesPage;
