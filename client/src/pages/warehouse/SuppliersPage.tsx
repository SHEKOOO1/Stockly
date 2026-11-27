import { Archive, ArchiveRestore, Eye, Pencil, Plus, Truck } from 'lucide-react';
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
import { useDemoSupplierStore, type SupplierFormValues } from '@/data/demoSupplierStore';
import { t, type TranslationKey } from '@/i18n';
import { formatDateTime, formatNumber } from '@/lib/format';
import {
  getSupplierDisplayName,
  type SupplierSummary,
  type SupplierValidationErrors,
} from '@/lib/supplier';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { Supplier } from '@/types/domain';

const formGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
  gap: '16px',
} as const;

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const actionRowStyle = {
  display: 'flex',
  justifyContent: 'flex-end',
  gap: '8px',
  flexWrap: 'wrap',
} as const;

const filterSelectStyle = { minWidth: 170 } as const;

type SupplierStatusFilter = '' | 'active' | 'archived';

type ValidationField = keyof SupplierValidationErrors;

function errorKey(
  field: ValidationField,
  error: SupplierValidationErrors[ValidationField],
): TranslationKey | undefined {
  if (!error) {
    return undefined;
  }
  if (error === 'required') {
    return 'warehouse.suppliers.validationRequired';
  }
  if (error === 'minLength') {
    return 'warehouse.suppliers.validationMinLength';
  }
  if (error === 'maxLength') {
    return 'warehouse.suppliers.validationMaxLength';
  }
  if (error === 'duplicate') {
    return 'warehouse.suppliers.validationDuplicate';
  }
  if (field === 'phone') {
    return 'warehouse.suppliers.validationPhone';
  }
  return 'warehouse.suppliers.validationEmail';
}

function emptyForm(): SupplierFormValues {
  return { name: '', code: '', contactName: '', phone: '', email: '', address: '', notes: '' };
}

function formFromSupplier(supplier: Supplier): SupplierFormValues {
  return {
    name: supplier.name,
    code: supplier.code,
    contactName: supplier.contactName,
    phone: supplier.phone,
    email: supplier.email,
    address: supplier.address,
    notes: supplier.notes,
  };
}

export function SuppliersPage() {
  const { selectedOrganization, selectedWarehouse, showToast } = useApp();
  const {
    suppliers,
    addSupplier,
    updateSupplier,
    archiveSupplier,
    restoreSupplier,
    getSupplierSummaries,
  } = useDemoSupplierStore();
  const loader = useDemoLoader(suppliers);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<SupplierStatusFilter>('');
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<Supplier | null>(null);
  const [details, setDetails] = useState<SupplierSummary | null>(null);
  const [archiveTarget, setArchiveTarget] = useState<Supplier | null>(null);
  const [form, setForm] = useState<SupplierFormValues>(emptyForm);
  const [errors, setErrors] = useState<SupplierValidationErrors>({});

  const summaries = useMemo(
    () => getSupplierSummaries(selectedOrganization.id),
    [getSupplierSummaries, selectedOrganization.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return summaries.filter((row) => {
      const matchesSearch =
        needle === '' ||
        [
          row.supplier.name,
          row.supplier.code,
          row.supplier.contactName,
          row.supplier.phone,
          row.supplier.email,
          row.supplier.address,
          row.supplier.notes,
        ]
          .join(' ')
          .toLocaleLowerCase()
          .includes(needle);
      const matchesStatus =
        statusFilter === '' ||
        (statusFilter === 'active' && !row.supplier.isDeleted) ||
        (statusFilter === 'archived' && row.supplier.isDeleted);
      return matchesSearch && matchesStatus;
    });
  }, [summaries, search, statusFilter]);

  const activeCount = summaries.filter((row) => !row.supplier.isDeleted).length;
  const archivedCount = summaries.filter((row) => row.supplier.isDeleted).length;
  const usedCount = summaries.filter((row) => row.usageCount > 0).length;
  const archivedSuffix = t('warehouse.suppliers.archivedSuffix');

  const columns = useMemo<DataTableColumn<SupplierSummary>[]>(
    () => [
      {
        id: 'name',
        headerKey: 'warehouse.suppliers.nameColumn',
        cell: (row) => (
          <strong>{getSupplierDisplayName(row.supplier, row.supplier.name, archivedSuffix)}</strong>
        ),
        sortValue: (row) => row.supplier.name,
      },
      {
        id: 'code',
        headerKey: 'warehouse.suppliers.codeColumn',
        cell: (row) => <span dir="ltr">{row.supplier.code}</span>,
        ltr: true,
        sortValue: (row) => row.supplier.code,
      },
      {
        id: 'contact',
        headerKey: 'warehouse.suppliers.contactColumn',
        cell: (row) => row.supplier.contactName,
        sortValue: (row) => row.supplier.contactName,
      },
      {
        id: 'phone',
        headerKey: 'warehouse.suppliers.phoneColumn',
        cell: (row) => (
          <span dir="ltr">{row.supplier.phone || t('common.states.notAvailable')}</span>
        ),
        ltr: true,
        sortValue: (row) => row.supplier.phone,
      },
      {
        id: 'usageCount',
        headerKey: 'warehouse.suppliers.usageCountColumn',
        cell: (row) => <span dir="ltr">{formatNumber(row.usageCount)}</span>,
        sortValue: (row) => row.usageCount,
        align: 'end',
        ltr: true,
      },
      {
        id: 'status',
        headerKey: 'warehouse.suppliers.statusColumn',
        cell: (row) => (
          <StatusBadge
            tone={row.supplier.isDeleted ? 'neutral' : 'healthy'}
            labelKey={row.supplier.isDeleted ? 'common.status.archived' : 'common.status.active'}
          />
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
              onClick={() => openEdit(row.supplier)}
              aria-label={t('common.actions.edit')}
            >
              <Pencil size={14} aria-hidden="true" />
              {t('common.actions.edit')}
            </button>
            {row.supplier.isDeleted ? (
              <button
                type="button"
                className="button button--quiet button--xs"
                onClick={() => {
                  restoreSupplier(row.supplier.id);
                  showToast('warehouse.suppliers.restoreToast', 'info');
                }}
              >
                <ArchiveRestore size={14} aria-hidden="true" />
                {t('warehouse.products.restoreAction')}
              </button>
            ) : (
              <button
                type="button"
                className="button button--quiet button--xs"
                onClick={() => setArchiveTarget(row.supplier)}
              >
                <Archive size={14} aria-hidden="true" />
                {t('warehouse.products.archiveAction')}
              </button>
            )}
          </div>
        ),
      },
    ],
    [archivedSuffix, restoreSupplier, showToast],
  );

  function openAdd() {
    setEditing(null);
    setAdding(true);
    setForm(emptyForm());
    setErrors({});
  }

  function openEdit(supplier: Supplier) {
    setAdding(false);
    setEditing(supplier);
    setForm(formFromSupplier(supplier));
    setErrors({});
  }

  function closeModal() {
    setAdding(false);
    setEditing(null);
    setErrors({});
  }

  function updateForm<K extends keyof SupplierFormValues>(key: K, value: SupplierFormValues[K]) {
    setForm((current) => ({ ...current, [key]: value }));
    setErrors((current) => ({ ...current, [key]: undefined }));
  }

  function saveForm() {
    const result = editing
      ? updateSupplier(editing.id, form)
      : addSupplier(selectedOrganization.id, form);
    if (result) {
      setErrors(result);
      return;
    }
    showToast(editing ? 'warehouse.shared.updateToast' : 'warehouse.shared.createToast', 'info');
    closeModal();
  }

  function confirmArchive() {
    if (!archiveTarget) {
      return;
    }
    archiveSupplier(archiveTarget.id);
    showToast('warehouse.suppliers.archiveToast', 'info');
    setArchiveTarget(null);
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
          labelKey="warehouse.suppliers.countKpi"
          value={formatNumber(summaries.length)}
          icon={Truck}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.suppliers.activeCount"
          value={formatNumber(activeCount)}
          icon={Truck}
          tone="healthy"
        />
        <StatCard
          labelKey="warehouse.suppliers.archivedCount"
          value={formatNumber(archivedCount)}
          icon={Archive}
          tone="neutral"
        />
        <StatCard
          labelKey="warehouse.suppliers.usageCountKpi"
          value={formatNumber(usedCount)}
          icon={Truck}
          tone="info"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.suppliers.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={() => {
            setSearch('');
            setStatusFilter('');
          }}
          filters={
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value as SupplierStatusFilter)}
              aria-label={t('warehouse.suppliers.statusFilter')}
              style={filterSelectStyle}
            >
              <option value="">{t('common.labels.all')}</option>
              <option value="active">{t('common.status.active')}</option>
              <option value="archived">{t('common.status.archived')}</option>
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
          rowKey={(row) => row.supplier.id}
          pageSize={8}
          captionKey="warehouse.suppliers.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={adding || editing !== null}
        onClose={closeModal}
        titleKey={editing ? 'warehouse.suppliers.editTitle' : 'warehouse.suppliers.addTitle'}
        descriptionKey="warehouse.shared.formDescription"
        size="lg"
        footer={
          <>
            <button type="button" className="button button--quiet" onClick={closeModal}>
              {t('common.actions.cancel')}
            </button>
            <button type="button" className="button button--primary" onClick={saveForm}>
              {t('warehouse.suppliers.savePreview')}
            </button>
          </>
        }
      >
        <div style={formGridStyle}>
          <FormField
            labelKey="warehouse.suppliers.nameField"
            required
            errorKey={errorKey('name', errors.name)}
          >
            <input value={form.name} onChange={(event) => updateForm('name', event.target.value)} />
          </FormField>
          <FormField
            labelKey="warehouse.suppliers.codeField"
            required
            errorKey={errorKey('code', errors.code)}
          >
            <input
              dir="ltr"
              value={form.code}
              onChange={(event) => updateForm('code', event.target.value)}
            />
          </FormField>
          <FormField
            labelKey="warehouse.suppliers.contactField"
            required
            errorKey={errorKey('contactName', errors.contactName)}
          >
            <input
              value={form.contactName}
              onChange={(event) => updateForm('contactName', event.target.value)}
            />
          </FormField>
          <FormField
            labelKey="warehouse.suppliers.phoneField"
            errorKey={errorKey('phone', errors.phone)}
          >
            <input
              type="tel"
              dir="ltr"
              value={form.phone}
              onChange={(event) => updateForm('phone', event.target.value)}
            />
          </FormField>
          <FormField
            labelKey="warehouse.suppliers.emailField"
            errorKey={errorKey('email', errors.email)}
          >
            <input
              type="email"
              dir="ltr"
              value={form.email}
              onChange={(event) => updateForm('email', event.target.value)}
            />
          </FormField>
          <FormField
            labelKey="warehouse.suppliers.addressField"
            errorKey={errorKey('address', errors.address)}
          >
            <input
              value={form.address}
              onChange={(event) => updateForm('address', event.target.value)}
            />
          </FormField>
          <FormField
            labelKey="warehouse.suppliers.notesField"
            errorKey={errorKey('notes', errors.notes)}
          >
            <textarea
              rows={3}
              value={form.notes}
              onChange={(event) => updateForm('notes', event.target.value)}
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
              <strong>{t('warehouse.suppliers.codeField')}</strong>
              <p dir="ltr">{details.supplier.code}</p>
            </div>
            <div>
              <strong>{t('warehouse.suppliers.contactField')}</strong>
              <p>{details.supplier.contactName}</p>
            </div>
            <div>
              <strong>{t('warehouse.suppliers.phoneField')}</strong>
              <p dir="ltr">{details.supplier.phone || t('common.states.notAvailable')}</p>
            </div>
            <div>
              <strong>{t('warehouse.suppliers.emailField')}</strong>
              <p dir="ltr">{details.supplier.email || t('common.states.notAvailable')}</p>
            </div>
            <div>
              <strong>{t('warehouse.suppliers.addressField')}</strong>
              <p>{details.supplier.address || t('common.states.notAvailable')}</p>
            </div>
            <div>
              <strong>{t('warehouse.suppliers.usageCountColumn')}</strong>
              <p dir="ltr">{formatNumber(details.usageCount)}</p>
            </div>
            <div>
              <strong>{t('common.labels.status')}</strong>
              <p>
                <StatusBadge
                  tone={details.supplier.isDeleted ? 'neutral' : 'healthy'}
                  labelKey={
                    details.supplier.isDeleted ? 'common.status.archived' : 'common.status.active'
                  }
                />
              </p>
            </div>
            <div>
              <strong>{t('warehouse.productDetails.updatedLabel')}</strong>
              <p dir="ltr">{formatDateTime(details.supplier.updatedAt)}</p>
            </div>
            <div>
              <strong>{t('warehouse.suppliers.notesField')}</strong>
              <p>{details.supplier.notes || t('common.states.notAvailable')}</p>
            </div>
          </div>
        ) : null}
      </Modal>

      <ConfirmDialog
        open={archiveTarget !== null}
        onClose={() => setArchiveTarget(null)}
        onConfirm={confirmArchive}
        titleKey="warehouse.suppliers.archiveTitle"
        messageKey="warehouse.suppliers.archiveMessage"
        confirmLabelKey="warehouse.products.archiveAction"
      />
    </>
  );
}

export default SuppliersPage;
