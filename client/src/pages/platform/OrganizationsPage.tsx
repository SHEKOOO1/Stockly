import { Eye, Pencil, Plus, Shield, Trash2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  ConfirmDialog,
  DataTable,
  DemoNotice,
  ErrorState,
  FormField,
  LoadingState,
  Modal,
  PageHeader,
  SearchFilterBar,
  StatusBadge,
  type DataTableColumn,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoOrganizations, demoPlans } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatDate, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { EntityStatus, Organization, StatusTone } from '@/types/domain';

const actionRowStyle = {
  display: 'flex',
  flexWrap: 'wrap',
  gap: '4px',
  justifyContent: 'flex-end',
} as const;

const formGridStyle = {
  display: 'grid',
  gap: '16px',
} as const;

const filterSelectStyle = { minWidth: 150 } as const;

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

const filterableStatuses: readonly EntityStatus[] = ['active', 'inactive', 'draft', 'pending'];

function planLabel(planId: string): string {
  return demoPlans.find((plan) => plan.id === planId)?.name ?? planId;
}

export function OrganizationsPage() {
  const navigate = useNavigate();
  const { showToast } = useApp();
  const loader = useDemoLoader(demoOrganizations);
  const [localOrganizations, setLocalOrganizations] =
    useState<readonly Organization[]>(demoOrganizations);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [planFilter, setPlanFilter] = useState('');
  const [editing, setEditing] = useState<Organization | null>(null);
  const [editForm, setEditForm] = useState({
    name: '',
    status: 'active' as EntityStatus,
    planId: '',
  });
  const [editNameError, setEditNameError] = useState(false);
  const [deleting, setDeleting] = useState<Organization | null>(null);

  const filteredRows = useMemo(
    () =>
      localOrganizations.filter((organization) => {
        const matchesSearch =
          search.trim() === '' ||
          `${organization.name} ${organization.code}`
            .toLocaleLowerCase()
            .includes(search.trim().toLocaleLowerCase());
        const matchesStatus = statusFilter === '' || organization.status === statusFilter;
        const matchesPlan = planFilter === '' || organization.planId === planFilter;
        return matchesSearch && matchesStatus && matchesPlan;
      }),
    [localOrganizations, search, statusFilter, planFilter],
  );

  const columns = useMemo<DataTableColumn<Organization>[]>(
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
        headerKey: 'platform.organizations.planColumn',
        cell: (row) => planLabel(row.planId),
        sortValue: (row) => planLabel(row.planId),
      },
      {
        id: 'status',
        headerKey: 'common.labels.status',
        cell: (row) => (
          <StatusBadge tone={statusTones[row.status]} labelKey={statusLabels[row.status]} />
        ),
      },
      {
        id: 'warehouseCount',
        headerKey: 'platform.organizations.warehousesColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.warehouseCount)}</span>,
        sortValue: (row) => row.warehouseCount,
        align: 'end',
      },
      {
        id: 'userCount',
        headerKey: 'platform.organizations.usersColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.userCount)}</span>,
        sortValue: (row) => row.userCount,
        align: 'end',
      },
      {
        id: 'createdAt',
        headerKey: 'platform.organizations.createdColumn',
        cell: (row) => <span dir="ltr">{formatDate(row.createdAt)}</span>,
        sortValue: (row) => row.createdAt,
      },
      {
        id: 'actions',
        headerKey: 'platform.organizations.actionsColumn',
        cell: (row) => (
          <div style={actionRowStyle}>
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => navigate(`/platform/organizations/${row.id}`)}
              aria-label={t('platform.organizations.viewAction')}
            >
              <Eye size={14} aria-hidden="true" />
              {t('platform.organizations.viewAction')}
            </button>
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => navigate(`/platform/organizations/${row.id}/administrators`)}
              aria-label={t('platform.organizations.administratorsAction')}
            >
              <Shield size={14} aria-hidden="true" />
              {t('platform.organizations.administratorsAction')}
            </button>
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => {
                setEditing(row);
                setEditForm({ name: row.name, status: row.status, planId: row.planId });
                setEditNameError(false);
              }}
              aria-label={t('platform.organizations.editAction')}
            >
              <Pencil size={14} aria-hidden="true" />
              {t('platform.organizations.editAction')}
            </button>
            <button
              type="button"
              className="button button--danger button--xs"
              onClick={() => setDeleting(row)}
              aria-label={t('platform.organizations.deleteAction')}
            >
              <Trash2 size={14} aria-hidden="true" />
              {t('platform.organizations.deleteAction')}
            </button>
          </div>
        ),
      },
    ],
    [navigate],
  );

  function resetFilters() {
    setSearch('');
    setStatusFilter('');
    setPlanFilter('');
  }

  function saveEdit() {
    if (editForm.name.trim().length < 3) {
      setEditNameError(true);
      return;
    }
    if (editing) {
      setLocalOrganizations((current) =>
        current.map((organization) =>
          organization.id === editing.id
            ? {
                ...organization,
                name: editForm.name.trim(),
                status: editForm.status,
                planId: editForm.planId,
              }
            : organization,
        ),
      );
      showToast('platform.organizations.editPreviewToast', 'info');
      setEditing(null);
    }
  }

  function confirmDelete() {
    if (!deleting) {
      return;
    }
    setLocalOrganizations((current) =>
      current.filter((organization) => organization.id !== deleting.id),
    );
    showToast('platform.organizations.deleteToast', 'info');
    setDeleting(null);
  }

  return (
    <>
      <PageHeader
        titleKey="platform.organizations.title"
        descriptionKey="platform.organizations.description"
        eyebrowKey="common.context.platform"
        actions={
          <Link className="button button--primary button--sm" to="/platform/organizations/new">
            <Plus size={14} aria-hidden="true" />
            {t('platform.organizations.newOrganization')}
          </Link>
        }
      />
      <DemoNotice compact />

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="platform.organizations.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <>
              <select
                value={statusFilter}
                onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
                aria-label={t('platform.organizations.statusFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {filterableStatuses.map((status) => (
                  <option key={status} value={status}>
                    {t(statusLabels[status])}
                  </option>
                ))}
              </select>
              <select
                value={planFilter}
                onChange={(event) => setPlanFilter(event.target.value)}
                aria-label={t('platform.organizations.planFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {demoPlans.map((plan) => (
                  <option key={plan.id} value={plan.id}>
                    {plan.name}
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
          captionKey="platform.organizations.title"
          emptyMessageKey="platform.organizations.emptyRows"
        />
      )}

      <Modal
        open={editing !== null}
        onClose={() => setEditing(null)}
        titleKey="platform.organizations.editTitle"
        descriptionKey="platform.organizations.editDescription"
        footer={
          <>
            <button type="button" className="button button--quiet" onClick={() => setEditing(null)}>
              {t('common.actions.cancel')}
            </button>
            <button type="button" className="button button--primary" onClick={saveEdit}>
              {t('platform.organizations.editSave')}
            </button>
          </>
        }
      >
        <div style={formGridStyle}>
          <FormField
            labelKey="platform.organizations.editName"
            required
            htmlFor="organization-edit-name"
            errorKey={editNameError ? 'common.form.fieldRequired' : undefined}
          >
            <input
              id="organization-edit-name"
              value={editForm.name}
              onChange={(event) => {
                setEditForm((current) => ({ ...current, name: event.target.value }));
                setEditNameError(false);
              }}
            />
          </FormField>
          <FormField
            labelKey="platform.organizations.editStatus"
            htmlFor="organization-edit-status"
          >
            <select
              id="organization-edit-status"
              value={editForm.status}
              onChange={(event) =>
                setEditForm((current) => ({
                  ...current,
                  status: event.target.value as EntityStatus,
                }))
              }
            >
              {filterableStatuses.map((status) => (
                <option key={status} value={status}>
                  {t(statusLabels[status])}
                </option>
              ))}
            </select>
          </FormField>
          <FormField labelKey="platform.organizations.editPlan" htmlFor="organization-edit-plan">
            <select
              id="organization-edit-plan"
              value={editForm.planId}
              onChange={(event) =>
                setEditForm((current) => ({ ...current, planId: event.target.value }))
              }
            >
              {demoPlans.map((plan) => (
                <option key={plan.id} value={plan.id}>
                  {plan.name}
                </option>
              ))}
            </select>
          </FormField>
        </div>
      </Modal>

      <ConfirmDialog
        open={deleting !== null}
        onClose={() => setDeleting(null)}
        onConfirm={confirmDelete}
        titleKey="platform.organizations.deleteTitle"
        messageKey="platform.organizations.deleteMessage"
        confirmLabelKey="common.actions.remove"
        tone="danger"
      />
    </>
  );
}

export default OrganizationsPage;
