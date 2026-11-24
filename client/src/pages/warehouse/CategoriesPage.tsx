import { Pencil, Plus, Tags } from 'lucide-react';
import { useMemo, useState } from 'react';
import {
  DataTable,
  DemoNotice,
  ErrorState,
  FormField,
  LoadingState,
  Modal,
  PageHeader,
  SearchFilterBar,
  StatCard,
  type DataTableColumn,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoCategories, demoDataOrigin, demoProducts } from '@/data/demoData';
import { t } from '@/i18n';
import { formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { Category } from '@/types/domain';

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
  justifyContent: 'flex-end',
} as const;

interface CategoryFormState {
  name: string;
  code: string;
  parentId: string;
}

interface CategoryNode {
  category: Category;
  depth: number;
}

function flattenCategories(
  rows: readonly Category[],
  parentId: string | null = null,
  depth = 0,
  visited: ReadonlySet<string> = new Set<string>(),
): CategoryNode[] {
  return rows
    .filter((category) => category.parentId === parentId && !visited.has(category.id))
    .flatMap((category) => {
      const nextVisited = new Set(visited);
      nextVisited.add(category.id);
      return [{ category, depth }, ...flattenCategories(rows, category.id, depth + 1, nextVisited)];
    });
}

export function CategoriesPage() {
  const { selectedWarehouse, showToast } = useApp();
  const loader = useDemoLoader(demoCategories);
  const [localCategories, setLocalCategories] = useState<readonly Category[]>(demoCategories);
  const [search, setSearch] = useState('');
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<Category | null>(null);
  const [form, setForm] = useState<CategoryFormState>({ name: '', code: '', parentId: '' });
  const [nameError, setNameError] = useState(false);
  const [codeError, setCodeError] = useState(false);

  const warehouseCategories = useMemo(
    () => localCategories.filter((category) => category.warehouseId === selectedWarehouse.id),
    [localCategories, selectedWarehouse.id],
  );

  const nodes = useMemo(() => flattenCategories(warehouseCategories), [warehouseCategories]);

  const filteredNodes = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    if (needle === '') {
      return nodes;
    }
    return nodes.filter((node) =>
      `${node.category.name} ${node.category.code}`.toLocaleLowerCase().includes(needle),
    );
  }, [nodes, search]);

  const totalProducts = useMemo(
    () => demoProducts.filter((product) => product.warehouseId === selectedWarehouse.id).length,
    [selectedWarehouse.id],
  );

  const columns: DataTableColumn<CategoryNode>[] = [
    {
      id: 'name',
      headerKey: 'warehouse.categories.hierarchyColumn',
      cell: (row) => (
        <strong style={{ paddingInlineStart: `${row.depth * 20}px` }}>
          {row.depth > 0 ? '— ' : ''}
          {row.category.name}
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
      cell: (row) => <span dir="ltr">{formatNumber(row.category.productCount)}</span>,
      sortValue: (row) => row.category.productCount,
      align: 'end',
      ltr: true,
    },
    {
      id: 'actions',
      headerKey: 'common.table.actions',
      cell: (row) => (
        <div style={actionRowStyle}>
          <button
            type="button"
            className="button button--quiet button--xs"
            onClick={() => openEdit(row.category)}
            aria-label={t('common.actions.edit')}
          >
            <Pencil size={14} aria-hidden="true" />
            {t('common.actions.edit')}
          </button>
        </div>
      ),
    },
  ];

  function openAdd() {
    setEditing(null);
    setAdding(true);
    setForm({ name: '', code: '', parentId: '' });
    setNameError(false);
    setCodeError(false);
  }

  function openEdit(category: Category) {
    setAdding(false);
    setEditing(category);
    setForm({
      name: category.name,
      code: category.code,
      parentId: category.parentId ?? '',
    });
    setNameError(false);
    setCodeError(false);
  }

  function closeModal() {
    setAdding(false);
    setEditing(null);
    setNameError(false);
    setCodeError(false);
  }

  function saveForm() {
    const nameValid = form.name.trim().length >= 3;
    const codeValid = form.code.trim().length >= 2;
    setNameError(!nameValid);
    setCodeError(!codeValid);
    if (!nameValid || !codeValid) {
      return;
    }

    const parentId = form.parentId === '' ? null : form.parentId;
    if (editing) {
      setLocalCategories((current) =>
        current.map((category) =>
          category.id === editing.id
            ? {
                ...category,
                name: form.name.trim(),
                code: form.code.trim(),
                parentId,
              }
            : category,
        ),
      );
      showToast('warehouse.shared.updateToast', 'info');
    } else {
      setLocalCategories((current) => [
        ...current,
        {
          origin: demoDataOrigin,
          id: `cat-local-${Date.now()}`,
          warehouseId: selectedWarehouse.id,
          name: form.name.trim(),
          code: form.code.trim(),
          productCount: 0,
          parentId,
        },
      ]);
      showToast('warehouse.shared.createToast', 'info');
    }
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
          labelKey="warehouse.overview.productsColumn"
          value={formatNumber(totalProducts)}
          icon={Tags}
          tone="healthy"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.categories.searchPlaceholder"
          resultCount={filteredNodes.length}
        />
      </div>

      {loader.status === 'error' ? (
        <ErrorState onRetry={loader.reload} />
      ) : loader.status === 'loading' ? (
        <LoadingState />
      ) : (
        <DataTable
          rows={filteredNodes}
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
              {t('warehouse.shared.savePreview')}
            </button>
          </>
        }
      >
        <div style={formGridStyle}>
          <FormField
            labelKey="warehouse.categories.nameField"
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
            labelKey="warehouse.categories.codeField"
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
          <FormField labelKey="warehouse.categories.parentField">
            <select
              value={form.parentId}
              onChange={(event) =>
                setForm((current) => ({ ...current, parentId: event.target.value }))
              }
            >
              <option value="">{t('warehouse.categories.rootOption')}</option>
              {warehouseCategories
                .filter((category) => category.id !== editing?.id)
                .map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
            </select>
          </FormField>
        </div>
      </Modal>
    </>
  );
}

export default CategoriesPage;
