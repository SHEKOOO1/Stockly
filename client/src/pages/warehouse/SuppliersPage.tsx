import { Check, Eye, Pencil, Plus, Truck } from 'lucide-react';
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
  StatCard,
  StatusBadge,
  type DataTableColumn,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoDataOrigin, demoSuppliers } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { EntityStatus, StatusTone, Supplier } from '@/types/domain';

const formGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
  gap: '16px',
} as const;

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const actionRowStyle = {
  display: 'flex',
  flexWrap: 'wrap',
  gap: '4px',
  justifyContent: 'flex-end',
} as const;

const filterSelectStyle = { minWidth: 150 } as const;

const statusFilters: readonly EntityStatus[] = ['active', 'inactive'];

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

interface SupplierFormState {
  name: string;
  code: string;
  contactName: string;
}

export function SuppliersPage() {
  const { selectedOrganization, selectedWarehouse, showToast } = useApp();
  const loader = useDemoLoader(demoSuppliers);
  const [localSuppliers, setLocalSuppliers] = useState<readonly Supplier[]>(demoSuppliers);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<Supplier | null>(null);
  const [details, setDetails] = useState<Supplier | null>(null);
  const [changingStatus, setChangingStatus] = useState<Supplier | null>(null);
  const [form, setForm] = useState<SupplierFormState>({ name: '', code: '', contactName: '' });
  const [nameError, setNameError] = useState(false);
  const [codeError, setCodeError] = useState(false);
  const [contactError, setContactError] = useState(false);

  const organizationSuppliers = useMemo(
    () => localSuppliers.filter((supplier) => supplier.organizationId === selectedOrganization.id),
    [localSuppliers, selectedOrganization.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return organizationSuppliers.filter((supplier) => {
      const matchesSearch =
        needle === '' ||
        `${supplier.name} ${supplier.code} ${supplier.contactName}`
          .toLocaleLowerCase()
          .includes(needle);
      const matchesStatus = statusFilter === '' || supplier.status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [organizationSuppliers, search, statusFilter]);

  const activeCount = useMemo(
    () => organizationSuppliers.filter((supplier) => supplier.status === 'active').length,
    [organizationSuppliers],
  );

  const columns: DataTableColumn<Supplier>[] = [
    {
      id: 'name',
      headerKey: 'common.labels.name',
      cell: (row) => <strong>{row.name}</strong>,
      sortValue: (row) => row.name,
    },
    {
      id: 'code',
      headerKey: 'warehouse.suppliers.codeColumn',
      cell: (row) => <span dir="ltr">{row.code}</span>,
      ltr: true,
      sortValue: (row) => row.code,
    },
    {
      id: 'contact',
      headerKey: 'warehouse.suppliers.contactColumn',
      cell: (row) => row.contactName,
      sortValue: (row) => row.contactName,
    },
    {
      id: 'status',
      headerKey: 'warehouse.suppliers.statusColumn',
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
          <button
            type="button"
            className="button button--quiet button--xs"
            onClick={() => openEdit(row)}
            aria-label={t('common.actions.edit')}
          >
            <Pencil size={14} aria-hidden="true" />
            {t('common.actions.edit')}
          </button>
          <button
            type="button"
            className="button button--quiet button--xs"
            onClick={() => setChangingStatus(row)}
            aria-label={t('warehouse.shared.changeStatusAction')}
          >
            <Check size={14} aria-hidden="true" />
            {t('warehouse.shared.changeStatusAction')}
          </button>
        </div>
      ),
    },
  ];

  function openAdd() {
    setEditing(null);
    setAdding(true);
    setForm({ name: '', code: '', contactName: '' });
    setNameError(false);
    setCodeError(false);
    setContactError(false);
  }

  function openEdit(supplier: Supplier) {
    setAdding(false);
    setEditing(supplier);
    setForm({
      name: supplier.name,
      code: supplier.code,
      contactName: supplier.contactName,
    });
    setNameError(false);
    setCodeError(false);
    setContactError(false);
  }

  function closeModal() {
    setAdding(false);
    setEditing(null);
    setNameError(false);
    setCodeError(false);
    setContactError(false);
  }

  function saveForm() {
    const nameValid = form.name.trim().length >= 3;
    const codeValid = form.code.trim().length >= 2;
    const contactValid = form.contactName.trim().length >= 2;
    setNameError(!nameValid);
    setCodeError(!codeValid);
    setContactError(!contactValid);
    if (!nameValid || !codeValid || !contactValid) {
      return;
    }

    if (editing) {
      setLocalSuppliers((current) =>
        current.map((supplier) =>
          supplier.id === editing.id
            ? {
                ...supplier,
                name: form.name.trim(),
                code: form.code.trim(),
                contactName: form.contactName.trim(),
              }
            : supplier,
        ),
      );
      showToast('warehouse.shared.updateToast', 'info');
    } else {
      setLocalSuppliers((current) => [
        ...current,
        {
          origin: demoDataOrigin,
          id: `sup-local-${Date.now()}`,
          organizationId: selectedOrganization.id,
          name: form.name.trim(),
          code: form.code.trim(),
          contactName: form.contactName.trim(),
          status: 'active',
        },
      ]);
      showToast('warehouse.shared.createToast', 'info');
    }
    closeModal();
  }

  function confirmStatusChange() {
    if (!changingStatus) {
      return;
    }
    const nextStatus: EntityStatus = changingStatus.status === 'active' ? 'inactive' : 'active';
    setLocalSuppliers((current) =>
      current.map((supplier) =>
        supplier.id === changingStatus.id ? { ...supplier, status: nextStatus } : supplier,
      ),
    );
    showToast('warehouse.shared.statusToast', 'info');
    setChangingStatus(null);
  }

  function resetFilters() {
    setSearch('');
    setStatusFilter('');
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.suppliers.title"
        descriptionKey="warehouse.suppliers.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
            <button type="button" className="button button--primary button--sm" onClick={openAdd}>
              <Plus size={14} aria-hidden="true" />
              {t('warehouse.suppliers.addSupplier')}
            </button>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.suppliers.title"
          value={formatNumber(organizationSuppliers.length)}
          icon={Truck}
          tone="info"
        />
        <StatCard
          labelKey="common.status.active"
          value={formatNumber(activeCount)}
          icon={Truck}
          tone="healthy"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.suppliers.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
              aria-label={t('warehouse.suppliers.statusFilter')}
              style={filterSelectStyle}
            >
              <option value="">{t('common.labels.all')}</option>
              {statusFilters.map((status) => (
                <option key={status} value={status}>
                  {t(statusLabels[status])}
                </option>
              ))}
            </select>
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
          pageSize={7}
          captionKey="warehouse.suppliers.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={adding || editing !== null}
        onClose={closeModal}
        titleKey={editing ? 'warehouse.suppliers.editTitle' : 'warehouse.suppliers.addTitle'}
        descriptionKey="warehouse.shared.formDescription"
        footer={
          <>
            <button type="button" className="button button--quiet" onClick={closeModal}>
              {t('common.actions.cancel')}
            </button>
            <button type="button" className="button button--primary" onClick={saveForm}>
              {t('warehouse.shared.savePreview')}
            </button>
          </>
        }
      >
        <div style={formGridStyle}>
          <FormField
            labelKey="warehouse.suppliers.nameField"
            required
            errorKey={nameError ? 'common.form.fieldRequired' : undefined}
          >
            <input
              value={form.name}
              onChange={(event) => {
                setForm((current) => ({ ...current, name: event.target.value }));
                setNameError(false);
              }}
            />
          </FormField>
          <FormField
            labelKey="warehouse.suppliers.codeField"
            required
            errorKey={codeError ? 'common.form.fieldRequired' : undefined}
          >
            <input
              dir="ltr"
              value={form.code}
              onChange={(event) => {
                setForm((current) => ({ ...current, code: event.target.value }));
                setCodeError(false);
              }}
            />
          </FormField>
          <FormField
            labelKey="warehouse.suppliers.contactField"
            required
            errorKey={contactError ? 'common.form.fieldRequired' : undefined}
          >
            <input
              value={form.contactName}
              onChange={(event) => {
                setForm((current) => ({ ...current, contactName: event.target.value }));
                setContactError(false);
              }}
            />
          </FormField>
        </div>
      </Modal>

      <Modal
        open={details !== null}
        onClose={() => setDetails(null)}
        titleKey="warehouse.suppliers.detailsTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={formGridStyle}>
            <div>
              <strong>{t('common.labels.code')}</strong>
              <p dir="ltr">{details.code}</p>
            </div>
            <div>
              <strong>{t('warehouse.suppliers.contactPanel')}</strong>
              <p>{details.contactName}</p>
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

      <ConfirmDialog
        open={changingStatus !== null}
        onClose={() => setChangingStatus(null)}
        onConfirm={confirmStatusChange}
        titleKey="warehouse.shared.confirmStatusTitle"
        messageKey="warehouse.shared.confirmStatusMessage"
      />
    </>
  );
}

export default SuppliersPage;
