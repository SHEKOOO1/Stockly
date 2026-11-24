import { Eye, Plus } from 'lucide-react';
import { useMemo, useState } from 'react';
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
import { demoRoles, demoUsers } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { DemoUser, EntityStatus, StatusTone } from '@/types/domain';

const filterSelectStyle = { minWidth: 150 } as const;

const summaryStyle = {
  display: 'grid',
  gap: '8px',
} as const;

const formGridStyle = {
  display: 'grid',
  gap: '16px',
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

interface InviteForm {
  name: string;
  email: string;
  roleName: string;
  warehouseId: string;
}

interface InviteErrors {
  name: boolean;
  email: boolean;
  roleName: boolean;
  warehouseId: boolean;
}

const emptyInviteErrors: InviteErrors = {
  name: false,
  email: false,
  roleName: false,
  warehouseId: false,
};

const emptyInviteForm: InviteForm = {
  name: '',
  email: '',
  roleName: '',
  warehouseId: '',
};

export function OrganizationUsersPage() {
  const { availableWarehouses, selectedOrganization, showToast } = useApp();
  const loader = useDemoLoader(demoUsers, { delayMs: 200 });
  const [users, setUsers] = useState<DemoUser[]>([...demoUsers]);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [selected, setSelected] = useState<DemoUser | null>(null);
  const [statusTarget, setStatusTarget] = useState<DemoUser | null>(null);
  const [inviteOpen, setInviteOpen] = useState(false);
  const [inviteForm, setInviteForm] = useState<InviteForm>(emptyInviteForm);
  const [inviteErrors, setInviteErrors] = useState<InviteErrors>(emptyInviteErrors);

  const scopedRoles = useMemo(
    () => demoRoles.filter((role) => role.organizationId === selectedOrganization.id),
    [selectedOrganization.id],
  );

  const warehouseNameById = useMemo(
    () => new Map(availableWarehouses.map((warehouse) => [warehouse.id, warehouse.name])),
    [availableWarehouses],
  );

  const scopedUsers = useMemo(
    () => users.filter((user) => user.organizationIds.includes(selectedOrganization.id)),
    [users, selectedOrganization.id],
  );

  const filteredUsers = useMemo(
    () =>
      scopedUsers.filter((user) => {
        const needle = search.trim().toLocaleLowerCase();
        const matchesSearch =
          needle === '' ||
          `${user.name} ${user.email} ${user.role}`.toLocaleLowerCase().includes(needle);
        const matchesRole = roleFilter === '' || user.role === roleFilter;
        const matchesStatus = statusFilter === '' || user.status === statusFilter;
        return matchesSearch && matchesRole && matchesStatus;
      }),
    [scopedUsers, search, roleFilter, statusFilter],
  );

  const columns = useMemo<DataTableColumn<DemoUser>[]>(
    () => [
      {
        id: 'name',
        headerKey: 'organization.users.nameColumn',
        cell: (row) => <strong>{row.name}</strong>,
        sortValue: (row) => row.name,
      },
      {
        id: 'email',
        headerKey: 'organization.users.emailColumn',
        cell: (row) => <span dir="ltr">{row.email}</span>,
        ltr: true,
        sortValue: (row) => row.email,
      },
      {
        id: 'role',
        headerKey: 'organization.users.roleColumn',
        cell: (row) => row.role,
        sortValue: (row) => row.role,
      },
      {
        id: 'status',
        headerKey: 'organization.users.statusColumn',
        cell: (row) => (
          <StatusBadge tone={statusTones[row.status]} labelKey={statusLabels[row.status]} />
        ),
      },
      {
        id: 'warehouses',
        headerKey: 'organization.users.warehousesColumn',
        cell: (row) =>
          row.warehouseIds
            .map((id) => warehouseNameById.get(id) ?? t('common.states.notAvailable'))
            .join(' · ') || t('common.states.notAvailable'),
      },
      {
        id: 'actions',
        headerKey: 'organization.users.actionsColumn',
        cell: (row) => (
          <div style={actionsStyle}>
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => setSelected(row)}
              aria-label={t('organization.users.detailsAction')}
            >
              <Eye size={14} aria-hidden="true" />
              {t('organization.users.detailsAction')}
            </button>
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => setStatusTarget(row)}
            >
              {t(
                row.status === 'active'
                  ? 'organization.users.deactivateAction'
                  : 'organization.users.activateAction',
              )}
            </button>
          </div>
        ),
      },
    ],
    [warehouseNameById],
  );

  function resetFilters() {
    setSearch('');
    setRoleFilter('');
    setStatusFilter('');
  }

  function openInvite() {
    setInviteForm({
      ...emptyInviteForm,
      roleName: scopedRoles[0]?.name ?? '',
      warehouseId: availableWarehouses[0]?.id ?? '',
    });
    setInviteErrors(emptyInviteErrors);
    setInviteOpen(true);
  }

  function submitInvite() {
    const nextErrors: InviteErrors = {
      name: inviteForm.name.trim().length < 3,
      email: !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(inviteForm.email.trim()),
      roleName: !scopedRoles.some((role) => role.name === inviteForm.roleName),
      warehouseId: !availableWarehouses.some(
        (warehouse) => warehouse.id === inviteForm.warehouseId,
      ),
    };
    setInviteErrors(nextErrors);
    if (Object.values(nextErrors).some(Boolean)) {
      return;
    }
    const nextUser: DemoUser = {
      origin: 'demo',
      id: `user-preview-${users.length + 1}`,
      name: inviteForm.name.trim(),
      role: inviteForm.roleName,
      email: inviteForm.email.trim(),
      organizationIds: [selectedOrganization.id],
      warehouseIds: [inviteForm.warehouseId],
      status: 'active',
    };
    setUsers((current) => [...current, nextUser]);
    setInviteOpen(false);
    setInviteForm(emptyInviteForm);
    showToast('organization.users.inviteToast', 'info');
  }

  function confirmStatusChange() {
    if (!statusTarget) {
      return;
    }
    const nextStatus: EntityStatus = statusTarget.status === 'active' ? 'inactive' : 'active';
    setUsers((current) =>
      current.map((user) => (user.id === statusTarget.id ? { ...user, status: nextStatus } : user)),
    );
    showToast('organization.users.statusToast', 'info');
    setStatusTarget(null);
  }

  return (
    <>
      <PageHeader
        titleKey="organization.users.title"
        descriptionKey="organization.users.description"
        eyebrowKey="common.context.organization"
        actions={
          <button type="button" className="button button--primary button--sm" onClick={openInvite}>
            <Plus size={14} aria-hidden="true" />
            {t('organization.users.inviteAction')}
          </button>
        }
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
              placeholderKey="organization.users.searchPlaceholder"
              resultCount={filteredUsers.length}
              onReset={resetFilters}
              filters={
                <>
                  <select
                    value={roleFilter}
                    onChange={(event) => setRoleFilter(event.target.value)}
                    aria-label={t('organization.users.roleFilter')}
                    style={filterSelectStyle}
                  >
                    <option value="">{t('common.labels.all')}</option>
                    {scopedRoles.map((role) => (
                      <option key={role.id} value={role.name}>
                        {role.name}
                      </option>
                    ))}
                  </select>
                  <select
                    value={statusFilter}
                    onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
                    aria-label={t('organization.users.statusFilter')}
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

          <DataTable
            rows={filteredUsers}
            columns={columns}
            rowKey={(row) => row.id}
            pageSize={6}
            captionKey="organization.users.title"
            emptyMessageKey="organization.users.emptyRows"
          />
        </>
      )}

      <Modal
        open={inviteOpen}
        onClose={() => setInviteOpen(false)}
        titleKey="organization.users.inviteTitle"
        descriptionKey="organization.users.inviteDescription"
        footer={
          <>
            <button
              type="button"
              className="button button--quiet"
              onClick={() => setInviteOpen(false)}
            >
              {t('common.actions.cancel')}
            </button>
            <button type="button" className="button button--primary" onClick={submitInvite}>
              {t('organization.users.inviteSubmit')}
            </button>
          </>
        }
      >
        <div style={formGridStyle}>
          <FormField
            labelKey="organization.users.inviteNameLabel"
            required
            htmlFor="invite-user-name"
            errorKey={inviteErrors.name ? 'organization.users.errorName' : undefined}
          >
            <input
              id="invite-user-name"
              value={inviteForm.name}
              onChange={(event) => {
                setInviteForm((current) => ({ ...current, name: event.target.value }));
                setInviteErrors((current) => ({ ...current, name: false }));
              }}
            />
          </FormField>
          <FormField
            labelKey="organization.users.inviteEmailLabel"
            required
            htmlFor="invite-user-email"
            errorKey={inviteErrors.email ? 'organization.users.errorEmail' : undefined}
          >
            <input
              id="invite-user-email"
              type="email"
              dir="ltr"
              value={inviteForm.email}
              onChange={(event) => {
                setInviteForm((current) => ({ ...current, email: event.target.value }));
                setInviteErrors((current) => ({ ...current, email: false }));
              }}
            />
          </FormField>
          <FormField
            labelKey="organization.users.inviteRoleLabel"
            required
            htmlFor="invite-user-role"
            errorKey={inviteErrors.roleName ? 'organization.users.errorRole' : undefined}
          >
            <select
              id="invite-user-role"
              value={inviteForm.roleName}
              onChange={(event) => {
                setInviteForm((current) => ({ ...current, roleName: event.target.value }));
                setInviteErrors((current) => ({ ...current, roleName: false }));
              }}
            >
              <option value="">{t('common.labels.all')}</option>
              {scopedRoles.map((role) => (
                <option key={role.id} value={role.name}>
                  {role.name}
                </option>
              ))}
            </select>
          </FormField>
          <FormField
            labelKey="organization.users.inviteWarehouseLabel"
            required
            htmlFor="invite-user-warehouse"
            errorKey={inviteErrors.warehouseId ? 'organization.users.errorWarehouse' : undefined}
          >
            <select
              id="invite-user-warehouse"
              value={inviteForm.warehouseId}
              onChange={(event) => {
                setInviteForm((current) => ({ ...current, warehouseId: event.target.value }));
                setInviteErrors((current) => ({ ...current, warehouseId: false }));
              }}
            >
              <option value="">{t('common.labels.all')}</option>
              {availableWarehouses.map((warehouse) => (
                <option key={warehouse.id} value={warehouse.id}>
                  {warehouse.name}
                </option>
              ))}
            </select>
          </FormField>
        </div>
      </Modal>

      <Modal
        open={selected !== null}
        onClose={() => setSelected(null)}
        titleKey="organization.users.detailsTitle"
        descriptionKey="organization.users.detailsDescription"
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
            <span>{t('organization.users.emailColumn')}</span>
            <strong dir="ltr">{selected?.email}</strong>
          </div>
          <div className="profile-detail">
            <span>{t('organization.users.roleColumn')}</span>
            <strong>{selected?.role}</strong>
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
            <span>{t('organization.users.warehousesColumn')}</span>
            <strong>
              {selected?.warehouseIds
                .map((id) => warehouseNameById.get(id) ?? t('common.states.notAvailable'))
                .join(' · ') ?? ''}
            </strong>
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
        titleKey="organization.users.statusConfirmTitle"
        messageKey={
          statusTarget?.status === 'active'
            ? 'organization.users.statusDeactivateMessage'
            : 'organization.users.statusActivateMessage'
        }
      />
    </>
  );
}

export default OrganizationUsersPage;
