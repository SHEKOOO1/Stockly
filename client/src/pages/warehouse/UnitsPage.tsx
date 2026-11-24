import { Pencil, Plus, Ruler } from 'lucide-react';
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
import { demoDataOrigin, demoUnits } from '@/data/demoData';
import { t } from '@/i18n';
import { formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { Unit } from '@/types/domain';

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

interface UnitFormState {
  name: string;
  code: string;
  decimalPlaces: string;
}

export function UnitsPage() {
  const { selectedWarehouse, showToast } = useApp();
  const loader = useDemoLoader(demoUnits);
  const [localUnits, setLocalUnits] = useState<readonly Unit[]>(demoUnits);
  const [search, setSearch] = useState('');
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<Unit | null>(null);
  const [form, setForm] = useState<UnitFormState>({ name: '', code: '', decimalPlaces: '0' });
  const [nameError, setNameError] = useState(false);
  const [codeError, setCodeError] = useState(false);
  const [decimalsError, setDecimalsError] = useState(false);

  const warehouseUnits = useMemo(
    () => localUnits.filter((unit) => unit.warehouseId === selectedWarehouse.id),
    [localUnits, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    if (needle === '') {
      return warehouseUnits;
    }
    return warehouseUnits.filter((unit) =>
      `${unit.name} ${unit.code}`.toLocaleLowerCase().includes(needle),
    );
  }, [warehouseUnits, search]);

  const maxDecimals = useMemo(
    () => warehouseUnits.reduce((max, unit) => Math.max(max, unit.decimalPlaces), 0),
    [warehouseUnits],
  );

  const columns: DataTableColumn<Unit>[] = [
    {
      id: 'name',
      headerKey: 'common.labels.name',
      cell: (row) => <strong>{row.name}</strong>,
      sortValue: (row) => row.name,
    },
    {
      id: 'code',
      headerKey: 'warehouse.units.codeColumn',
      cell: (row) => <span dir="ltr">{row.code}</span>,
      ltr: true,
      sortValue: (row) => row.code,
    },
    {
      id: 'decimals',
      headerKey: 'warehouse.units.decimalsColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.decimalPlaces)}</span>,
      sortValue: (row) => row.decimalPlaces,
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
            onClick={() => openEdit(row)}
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
    setForm({ name: '', code: '', decimalPlaces: '0' });
    setNameError(false);
    setCodeError(false);
    setDecimalsError(false);
  }

  function openEdit(unit: Unit) {
    setAdding(false);
    setEditing(unit);
    setForm({
      name: unit.name,
      code: unit.code,
      decimalPlaces: String(unit.decimalPlaces),
    });
    setNameError(false);
    setCodeError(false);
    setDecimalsError(false);
  }

  function closeModal() {
    setAdding(false);
    setEditing(null);
    setNameError(false);
    setCodeError(false);
    setDecimalsError(false);
  }

  function saveForm() {
    const nameValid = form.name.trim().length >= 2;
    const codeValid = form.code.trim().length >= 2;
    const decimals = Number(form.decimalPlaces);
    const decimalsValid =
      form.decimalPlaces.trim() !== '' &&
      Number.isInteger(decimals) &&
      decimals >= 0 &&
      decimals <= 4;
    setNameError(!nameValid);
    setCodeError(!codeValid);
    setDecimalsError(!decimalsValid);
    if (!nameValid || !codeValid || !decimalsValid) {
      return;
    }

    if (editing) {
      setLocalUnits((current) =>
        current.map((unit) =>
          unit.id === editing.id
            ? {
                ...unit,
                name: form.name.trim(),
                code: form.code.trim(),
                decimalPlaces: decimals,
              }
            : unit,
        ),
      );
      showToast('warehouse.shared.updateToast', 'info');
    } else {
      setLocalUnits((current) => [
        ...current,
        {
          origin: demoDataOrigin,
          id: `unit-local-${Date.now()}`,
          warehouseId: selectedWarehouse.id,
          name: form.name.trim(),
          code: form.code.trim(),
          decimalPlaces: decimals,
        },
      ]);
      showToast('warehouse.shared.createToast', 'info');
    }
    closeModal();
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.units.title"
        descriptionKey="warehouse.units.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
            <button type="button" className="button button--primary button--sm" onClick={openAdd}>
              <Plus size={14} aria-hidden="true" />
              {t('warehouse.units.addUnit')}
            </button>
          </>
        }
      />
      <DemoNotice compact messageKey="warehouse.shared.localNotice" />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.units.countKpi"
          value={formatNumber(warehouseUnits.length)}
          icon={Ruler}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.units.decimalsColumn"
          value={formatNumber(maxDecimals)}
          icon={Ruler}
          tone="healthy"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.units.searchPlaceholder"
          resultCount={filteredRows.length}
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
          pageSize={8}
          captionKey="warehouse.units.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={adding || editing !== null}
        onClose={closeModal}
        titleKey={editing ? 'warehouse.units.editTitle' : 'warehouse.units.addTitle'}
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
            labelKey="warehouse.units.nameField"
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
            labelKey="warehouse.units.codeField"
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
            labelKey="warehouse.units.decimalsField"
            required
            errorKey={decimalsError ? 'common.form.invalidNumber' : undefined}
          >
            <input
              type="number"
              min="0"
              max="4"
              dir="ltr"
              value={form.decimalPlaces}
              onChange={(event) => {
                setForm((current) => ({ ...current, decimalPlaces: event.target.value }));
                setDecimalsError(false);
              }}
            />
          </FormField>
        </div>
      </Modal>
    </>
  );
}

export default UnitsPage;
