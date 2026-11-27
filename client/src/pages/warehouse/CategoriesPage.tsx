import { Archive, ArchiveRestore, Eye, Pencil, Plus, Tags } from 'lucide-react';
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
import { useDemoCategoryStore, type CategorySummaryRow } from '@/data/demoCategoryStore';
import { useDemoProductStore } from '@/data/demoProductStore';
import { t } from '@/i18n';
import { getCategoryDisplayName, type CategoryValidationErrors } from '@/lib/category';
import { formatDateTime, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { Category } from '@/types/domain';

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

interface CategoryFormState {
  name: string;
  code: string;
  parentId: string;
}

type CategoryStatusFilter = '' | 'active' | 'archived';

function errorKey(error: CategoryValidationErrors[keyof CategoryValidationErrors]) {
  if (error === 'required') return 'warehouse.categories.validationRequired';
  if (error === 'minLength') return 'warehouse.categories.validationMinLength';
  if (error === 'duplicate') return 'warehouse.categories.validationDuplicate';
  if (error === 'self') return 'warehouse.categories.validationSelfParent';
  if (error === 'cycle') return 'warehouse.categories.validationCycle';
  return undefined;
}

export function CategoriesPage() {
  const { selectedWarehouse, showToast } = useApp();
  const { products } = useDemoProductStore();
  const {
    categories,
    getCategorySummaries,
    addCategory,
    updateCategory,
    archiveCategory,
    restoreCategory,
    getSelectableCategories,
  } = useDemoCategoryStore();
  const loader = useDemoLoader(categories);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<CategoryStatusFilter>('');
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<Category | null>(null);
  const [details, setDetails] = useState<CategorySummaryRow | null>(null);
  const [archiveTarget, setArchiveTarget] = useState<Category | null>(null);
  const [form, setForm] = useState<CategoryFormState>({ name: '', code: '', parentId: '' });
  const [errors, setErrors] = useState<CategoryValidationErrors>({});

  const warehouseCategories = useMemo(
    () => categories.filter((category) => category.warehouseId === selectedWarehouse.id),
    [categories, selectedWarehouse.id],
  );
  const summaries = useMemo(
    () => getCategorySummaries(selectedWarehouse.id),
    [getCategorySummaries, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return summaries.filter((row) => {
      const matchesSearch =
        needle === '' ||
        `${row.category.name} ${row.category.code}`.toLocaleLowerCase().includes(needle);
      const matchesStatus =
        statusFilter === '' ||
        (statusFilter === 'active' && !row.category.isDeleted) ||
        (statusFilter === 'archived' && row.category.isDeleted);
      return matchesSearch && matchesStatus;
    });
  }, [summaries, search, statusFilter]);

  const activeCount = warehouseCategories.filter((category) => !category.isDeleted).length;
  const archivedCount = warehouseCategories.filter((category) => category.isDeleted).length;
  const assignedProducts = products.filter(
    (product) => product.warehouseId === selectedWarehouse.id && !product.isDeleted,
  ).length;

  const parentOptions = useMemo(
    () => getSelectableCategories(selectedWarehouse.id, editing?.id ?? null),
    [getSelectableCategories, selectedWarehouse.id, editing?.id],
  );

  const columns: DataTableColumn<CategorySummaryRow>[] = [
    {
      id: 'name',
      headerKey: 'warehouse.categories.hierarchyColumn',
      cell: (row) => (
        <strong style={{ paddingInlineStart: `${row.depth * 20}px` }}>
          {row.depth > 0 ? '└ ' : ''}
          {getCategoryDisplayName(
            row.category,
            row.category.name,
            t('warehouse.shared.archivedCategorySuffix'),
          )}
        </strong>
      ),
      sortValue: (row) => row.category.name,
    },
    {
      id: 'code',
      headerKey: 'warehouse.categories.codeColumn',
      cell: (row) => <span dir="ltr">{row.category.code}</span>,
      ltr: true,
      sortValue: (row) => row.category.code,
    },
    {
      id: 'productCount',
      headerKey: 'warehouse.categories.productCountColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.productCount)}</span>,
      sortValue: (row) => row.productCount,
      align: 'end',
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'common.labels.status',
      cell: (row) => (
        <StatusBadge
          tone={row.category.isDeleted ? 'neutral' : 'healthy'}
          labelKey={row.category.isDeleted ? 'common.status.archived' : 'common.status.active'}
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
            onClick={() => openEdit(row.category)}
            aria-label={t('common.actions.edit')}
          >
            <Pencil size={14} aria-hidden="true" />
            {t('common.actions.edit')}
          </button>
          {row.category.isDeleted ? (
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => {
                restoreCategory(row.category.id);
                showToast('warehouse.categories.restoreToast', 'info');
              }}
            >
              <ArchiveRestore size={14} aria-hidden="true" />
              {t('warehouse.products.restoreAction')}
            </button>
          ) : (
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => setArchiveTarget(row.category)}
            >
              <Archive size={14} aria-hidden="true" />
              {t('warehouse.products.archiveAction')}
            </button>
          )}
        </div>
      ),
    },
  ];

  function openAdd() {
    setEditing(null);
    setAdding(true);
    setForm({ name: '', code: '', parentId: '' });
    setErrors({});
  }

  function openEdit(category: Category) {
    setAdding(false);
    setEditing(category);
    setForm({ name: category.name, code: category.code, parentId: category.parentId ?? '' });
    setErrors({});
  }

  function closeModal() {
    setAdding(false);
    setEditing(null);
    setErrors({});
  }

  function saveForm() {
    const result = editing
      ? updateCategory(editing.id, form)
      : addCategory(selectedWarehouse.id, form);
    if (result) {
      setErrors(result);
      return;
    }
    showToast(editing ? 'warehouse.shared.updateToast' : 'warehouse.shared.createToast', 'info');
    closeModal();
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.categories.title"
        descriptionKey="warehouse.categories.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
            <button type="button" className="button button--primary button--sm" onClick={openAdd}>
              <Plus size={14} aria-hidden="true" />
              {t('warehouse.categories.addCategory')}
            </button>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.overview.categoriesKpi"
          value={formatNumber(warehouseCategories.length)}
          icon={Tags}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.categories.activeCount"
          value={formatNumber(activeCount)}
          icon={Tags}
          tone="healthy"
        />
        <StatCard
          labelKey="warehouse.categories.archivedCount"
          value={formatNumber(archivedCount)}
          icon={Archive}
          tone="neutral"
        />
        <StatCard
          labelKey="warehouse.categories.assignedProducts"
          value={formatNumber(assignedProducts)}
          icon={Tags}
          tone="info"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.categories.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={() => {
            setSearch('');
            setStatusFilter('');
          }}
          filters={
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value as CategoryStatusFilter)}
              aria-label={t('common.labels.status')}
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
          rowKey={(row) => row.category.id}
          pageSize={8}
          captionKey="warehouse.categories.treeTitle"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={adding || editing !== null}
        onClose={closeModal}
        titleKey={editing ? 'warehouse.categories.editTitle' : 'warehouse.categories.addTitle'}
        descriptionKey="warehouse.shared.formDescription"
        footer={
          <>
            <button type="button" className="button button--quiet" onClick={closeModal}>
              {t('common.actions.cancel')}
            </button>
            <button type="button" className="button button--primary" onClick={saveForm}>
              {t('warehouse.categories.savePreview')}
            </button>
          </>
        }
      >
        <div style={formGridStyle}>
          <FormField
            labelKey="warehouse.categories.nameField"
            required
            errorKey={errorKey(errors.name)}
          >
            <input
              value={form.name}
              onChange={(event) => {
                setForm((current) => ({ ...current, name: event.target.value }));
                setErrors((c) => ({ ...c, name: undefined }));
              }}
            />
          </FormField>
          <FormField
            labelKey="warehouse.categories.codeField"
            required
            errorKey={errorKey(errors.code)}
          >
            <input
              dir="ltr"
              value={form.code}
              onChange={(event) => {
                setForm((current) => ({ ...current, code: event.target.value }));
                setErrors((c) => ({ ...c, code: undefined }));
              }}
            />
          </FormField>
          <FormField
            labelKey="warehouse.categories.parentField"
            errorKey={errorKey(errors.parentId)}
          >
            <select
              value={form.parentId}
              onChange={(event) => {
                setForm((current) => ({ ...current, parentId: event.target.value }));
                setErrors((c) => ({ ...c, parentId: undefined }));
              }}
            >
              <option value="">{t('warehouse.categories.rootOption')}</option>
              {parentOptions.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
              {editing?.parentId && !parentOptions.some((c) => c.id === editing.parentId) ? (
                <option value={editing.parentId}>
                  {warehouseCategories.find((c) => c.id === editing.parentId)?.name ??
                    editing.parentId}
                </option>
              ) : null}
            </select>
          </FormField>
        </div>
      </Modal>

      <Modal
        open={details !== null}
        onClose={() => setDetails(null)}
        titleKey="warehouse.categories.detailsTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={formGridStyle}>
            <div>
              <strong>{t('warehouse.categories.nameField')}</strong>
              <p>{details.category.name}</p>
            </div>
            <div>
              <strong>{t('warehouse.categories.codeField')}</strong>
              <p dir="ltr">{details.category.code}</p>
            </div>
            <div>
              <strong>{t('warehouse.categories.productCountColumn')}</strong>
              <p dir="ltr">{formatNumber(details.productCount)}</p>
            </div>
            <div>
              <strong>{t('common.labels.status')}</strong>
              <p>
                <StatusBadge
                  tone={details.category.isDeleted ? 'neutral' : 'healthy'}
                  labelKey={
                    details.category.isDeleted ? 'common.status.archived' : 'common.status.active'
                  }
                />
              </p>
            </div>
            <div>
              <strong>{t('warehouse.categories.parentField')}</strong>
              <p>
                {warehouseCategories.find((c) => c.id === details.category.parentId)?.name ??
                  t('warehouse.categories.rootOption')}
              </p>
            </div>
            <div>
              <strong>{t('warehouse.productDetails.updatedLabel')}</strong>
              <p dir="ltr">{formatDateTime(details.category.updatedAt)}</p>
            </div>
          </div>
        ) : null}
      </Modal>

      <ConfirmDialog
        open={archiveTarget !== null}
        onClose={() => setArchiveTarget(null)}
        onConfirm={() => {
          if (archiveTarget) {
            archiveCategory(archiveTarget.id);
            showToast('warehouse.categories.archiveToast', 'info');
          }
        }}
        titleKey="warehouse.categories.archiveTitle"
        messageKey="warehouse.categories.archiveMessage"
        confirmLabelKey="warehouse.products.archiveAction"
        tone="danger"
      />
    </>
  );
}

export default CategoriesPage;
