import { Plus, Trash2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  ConfirmDialog,
  DataTable,
  DemoNotice,
  EmptyState,
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
import { demoUsers } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { DemoUser, EntityStatus, StatusTone } from '@/types/domain';

const actionRowStyle = {
  display: 'flex',
  justifyContent: 'flex-end',
} as const;

const formGridStyle = {
  display: 'grid',
  gap: '16px',
} as const;

type InviteRole = 'owner' | 'manager' | 'auditor';

const roleLabels: Record<InviteRole, TranslationKey> = {
  owner: 'platform.organizationAdministrators.roleOwner',
  manager: 'platform.organizationAdministrators.roleManager',
  auditor: 'platform.organizationAdministrators.roleAuditor',
};

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

export function OrganizationAdministratorsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { organizations, showToast } = useApp();
  const loader = useDemoLoader(demoUsers);
  const [admins, setAdmins] = useState<readonly DemoUser[]>(demoUsers);
  const [search, setSearch] = useState('');
  const [inviteOpen, setInviteOpen] = useState(false);
  const [inviteForm, setInviteForm] = useState({
    name: '',
    email: '',
    role: 'owner' as InviteRole,
  });
  const [inviteErrors, setInviteErrors] = useState({ name: false, email: false });
  const [removing, setRemoving] = useState<DemoUser | null>(null);

  const organization = useMemo(
    () => organizations.find((item) => item.id === id),
    [organizations, id],
  );

  const filteredRows = useMemo(
    () =>
      admins.filter((admin) => {
        if (!admin.organizationIds.includes(id ?? '')) {
          return false;
        }
        const needle = search.trim().toLocaleLowerCase();
        return needle === '' || `${admin.name} ${admin.email}`.toLocaleLowerCase().includes(needle);
      }),
    [admins, id, search],
  );

  const columns = useMemo<DataTableColumn<DemoUser>[]>(
    () => [
      {
        id: 'name',
        headerKey: 'platform.organizationAdministrators.nameColumn',
        cell: (row) => <strong>{row.name}</strong>,
        sortValue: (row) => row.name,
      },
      {
        id: 'email',
        headerKey: 'platform.organizationAdministrators.emailColumn',
        cell: (row) => <span dir="ltr">{row.email}</span>,
        ltr: true,
        sortValue: (row) => row.email,
      },
      {
        id: 'role',
        headerKey: 'platform.organizationAdministrators.roleColumn',
        cell: (row) => row.role,
        sortValue: (row) => row.role,
      },
      {
        id: 'status',
        headerKey: 'platform.organizationAdministrators.statusColumn',
        cell: (row) => (
          <StatusBadge tone={statusTones[row.status]} labelKey={statusLabels[row.status]} />
        ),
      },
      {
        id: 'warehouses',
        headerKey: 'platform.organizationAdministrators.warehousesColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.warehouseIds.length)}</span>,
        sortValue: (row) => row.warehouseIds.length,
        align: 'end',
      },
      {
        id: 'actions',
        headerKey: 'platform.organizationAdministrators.actionsColumn',
        cell: (row) => (
          <div style={actionRowStyle}>
            <button
              type="button"
              className="button button--danger button--xs"
              onClick={() => setRemoving(row)}
              aria-label={t('platform.organizationAdministrators.removeAction')}
            >
              <Trash2 size={14} aria-hidden="true" />
              {t('platform.organizationAdministrators.removeAction')}
            </button>
          </div>
        ),
      },
    ],
    [],
  );

  function resetInvite() {
    setInviteForm({ name: '', email: '', role: 'owner' });
    setInviteErrors({ name: false, email: false });
  }

  function submitInvite() {
    const nameValid = inviteForm.name.trim().length > 1;
    const emailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(inviteForm.email.trim());
    setInviteErrors({ name: !nameValid, email: !emailValid });
    if (!nameValid || !emailValid || !id) {
      return;
    }
    const invited: DemoUser = {
      origin: 'demo',
      id: `invite-${Date.now()}`,
      name: inviteForm.name.trim(),
      role: t(roleLabels[inviteForm.role]),
      roleCode: inviteForm.role === 'owner' ? 'TenantOwner' : 'WarehouseManager',
      email: inviteForm.email.trim(),
      organizationIds: [id],
      warehouseIds: [],
      status: 'active',
    };
    setAdmins((current) => [...current, invited]);
    showToast('platform.organizationAdministrators.inviteToast', 'info');
    resetInvite();
    setInviteOpen(false);
  }

  function confirmRemove() {
    if (!removing) {
      return;
    }
    setAdmins((current) => current.filter((admin) => admin.id !== removing.id));
    showToast('platform.organizationAdministrators.removeToast', 'info');
    setRemoving(null);
  }

  if (!organization) {
    return (
      <>
        <PageHeader
          titleKey="platform.organizationAdministrators.title"
          descriptionKey="platform.organizationAdministrators.description"
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

  return (
    <>
      <PageHeader
        titleKey="platform.organizationAdministrators.title"
        descriptionKey="platform.organizationAdministrators.description"
        eyebrowKey="common.context.organization"
        actions={
          <>
            <Link
              className="button button--quiet button--sm"
              to={`/platform/organizations/${organization.id}`}
            >
              {t('platform.organizations.viewAction')}
            </Link>
            <button
              type="button"
              className="button button--primary button--sm"
              onClick={() => setInviteOpen(true)}
            >
              <Plus size={14} aria-hidden="true" />
              {t('platform.organizationAdministrators.inviteAdmin')}
            </button>
          </>
        }
      />
      <DemoNotice compact />

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="platform.organizationAdministrators.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={() => setSearch('')}
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
          pageSize={5}
          captionKey="platform.organizationAdministrators.title"
          emptyMessageKey="platform.organizationAdministrators.emptyRows"
        />
      )}

      <Modal
        open={inviteOpen}
        onClose={() => setInviteOpen(false)}
        titleKey="platform.organizationAdministrators.inviteTitle"
        descriptionKey="platform.organizationAdministrators.inviteDescription"
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
              {t('platform.organizationAdministrators.inviteSubmit')}
            </button>
          </>
        }
      >
        <div style={formGridStyle}>
          <FormField
            labelKey="platform.organizationAdministrators.nameLabel"
            required
            htmlFor="admin-invite-name"
            errorKey={
              inviteErrors.name ? 'platform.organizationAdministrators.errorName' : undefined
            }
          >
            <input
              id="admin-invite-name"
              value={inviteForm.name}
              onChange={(event) => {
                setInviteForm((current) => ({ ...current, name: event.target.value }));
                setInviteErrors((current) => ({ ...current, name: false }));
              }}
            />
          </FormField>
          <FormField
            labelKey="platform.organizationAdministrators.emailLabel"
            required
            htmlFor="admin-invite-email"
            errorKey={
              inviteErrors.email ? 'platform.organizationAdministrators.errorEmail' : undefined
            }
          >
            <input
              id="admin-invite-email"
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
            labelKey="platform.organizationAdministrators.roleLabel"
            htmlFor="admin-invite-role"
          >
            <select
              id="admin-invite-role"
              value={inviteForm.role}
              onChange={(event) =>
                setInviteForm((current) => ({
                  ...current,
                  role: event.target.value as InviteRole,
                }))
              }
            >
              {(Object.keys(roleLabels) as InviteRole[]).map((role) => (
                <option key={role} value={role}>
                  {t(roleLabels[role])}
                </option>
              ))}
            </select>
          </FormField>
        </div>
      </Modal>

      <ConfirmDialog
        open={removing !== null}
        onClose={() => setRemoving(null)}
        onConfirm={confirmRemove}
        titleKey="platform.organizationAdministrators.removeTitle"
        messageKey="platform.organizationAdministrators.removeMessage"
        confirmLabelKey="common.actions.remove"
        tone="danger"
      />
    </>
  );
}

export default OrganizationAdministratorsPage;
