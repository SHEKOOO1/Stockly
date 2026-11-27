import { Archive, ArchiveRestore, Eye, Pencil, Plus, Ruler } from 'lucide-react';
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
import { useDemoUnitStore } from '@/data/demoUnitStore';
import { t, type TranslationKey } from '@/i18n';
import {
  getUnitDisplayName,
  unitTypeLabelKeys,
  type UnitSummary,
  type UnitValidationErrors,
} from '@/lib/unit';
import { formatDateTime, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { Unit, UnitType } from '@/types/domain';

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

type UnitStatusFilter = '' | 'active' | 'archived';

interface UnitFormState {
  name: string;
  code: string;
  decimalPlaces: string;
  unitType: UnitType;
}

const unitTypeOptions = Object.keys(unitTypeLabelKeys) as UnitType[];

function errorKey(
  error: UnitValidationErrors[keyof UnitValidationErrors],
): TranslationKey | undefined {
  if (error === 'required') return 'warehouse.units.validationRequired';
  if (error === 'minLength') return 'warehouse.units.validationMinLength';
  if (error === 'duplicate') return 'warehouse.units.validationDuplicate';
  if (error === 'invalid' || error === 'range') return 'warehouse.units.validationDecimals';
  return 'warehouse.units.validationType';
}

export function UnitsPage() {
  const { selectedWarehouse, showToast } = useApp();
  const { units, addUnit, updateUnit, archiveUnit, restoreUnit, getUnitSummaries, getUsageCount } =
    useDemoUnitStore();
  const loader = useDemoLoader(units);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<UnitStatusFilter>('');
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<Unit | null>(null);
  const [details, setDetails] = useState<UnitSummary | null>(null);
  const [archiveTarget, setArchiveTarget] = useState<Unit | null>(null);
  const [blockedUnit, setBlockedUnit] = useState<{ unit: Unit; count: number } | null>(null);
  const [form, setForm] = useState<UnitFormState>({
    name: '',
    code: '',
    decimalPlaces: '0',
    unitType: 'count',
  });
  const [errors, setErrors] = useState<UnitValidationErrors>({});

  const warehouseUnits = useMemo(
    () => units.filter((unit) => unit.warehouseId === selectedWarehouse.id),
    [units, selectedWarehouse.id],
  );
  const summaries = useMemo(
    () => getUnitSummaries(selectedWarehouse.id),
    [getUnitSummaries, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return summaries.filter((row) => {
      const matchesSearch =
        needle === '' ||
        `${row.unit.name} ${row.unit.code} ${t(unitTypeLabelKeys[row.unit.unitType])}`
          .toLocaleLowerCase()
          .includes(needle);
      const matchesStatus =
        statusFilter === '' ||
        (statusFilter === 'active' && !row.unit.isDeleted) ||
        (statusFilter === 'archived' && row.unit.isDeleted);
      return matchesSearch && matchesStatus;
    });
  }, [summaries, search, statusFilter]);

  const activeCount = warehouseUnits.filter((unit) => !unit.isDeleted).length;
  const archivedCount = warehouseUnits.filter((unit) => unit.isDeleted).length;
  const usedCount = summaries.filter((row) => row.usageCount > 0).length;

  const columns: DataTableColumn<UnitSummary>[] = [
    {
      id: 'name',
      headerKey: 'warehouse.units.nameColumn',
      cell: (row) => (
        <strong>
          {getUnitDisplayName(row.unit, row.unit.name, t('warehouse.shared.archivedUnitSuffix'))}
        </strong>
      ),
      sortValue: (row) => row.unit.name,
    },
    {
      id: 'code',
      headerKey: 'warehouse.units.codeColumn',
      cell: (row) => <span dir="ltr">{row.unit.code}</span>,
      ltr: true,
      sortValue: (row) => row.unit.code,
    },
    {
      id: 'type',
      headerKey: 'warehouse.units.typeColumn',
      cell: (row) => t(unitTypeLabelKeys[row.unit.unitType]),
      sortValue: (row) => t(unitTypeLabelKeys[row.unit.unitType]),
    },
    {
      id: 'decimals',
      headerKey: 'warehouse.units.decimalsColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.unit.decimalPlaces)}</span>,
      sortValue: (row) => row.unit.decimalPlaces,
      align: 'end',
      ltr: true,
    },
    {
      id: 'usageCount',
      headerKey: 'warehouse.units.usageCountColumn',
      cell: (row) => <span dir="ltr">{formatNumber(row.usageCount)}</span>,
      sortValue: (row) => row.usageCount,
      align: 'end',
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'common.labels.status',
      cell: (row) => (
        <StatusBadge
          tone={row.unit.isDeleted ? 'neutral' : 'healthy'}
          labelKey={row.unit.isDeleted ? 'common.status.archived' : 'common.status.active'}
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
            onClick={() => openEdit(row.unit)}
            aria-label={t('common.actions.edit')}
          >
            <Pencil size={14} aria-hidden="true" />
            {t('common.actions.edit')}
          </button>
          {row.unit.isDeleted ? (
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => {
                restoreUnit(row.unit.id);
                showToast('warehouse.units.restoreToast', 'info');
              }}
            >
              <ArchiveRestore size={14} aria-hidden="true" />
              {t('warehouse.products.restoreAction')}
            </button>
          ) : (
            <button
              type="button"
              className="button button--quiet button--xs"
              onClick={() => requestArchive(row.unit)}
            >
              <Archive size={14} aria-hidden="true" />
              {t('warehouse.products.archiveAction')}
            </button>
          )}
        </div>
      ),
    },
  ];

  function requestArchive(unit: Unit) {
    const count = getUsageCount(unit.id);
    if (count > 0) {
      setBlockedUnit({ unit, count });
      return;
    }
    setArchiveTarget(unit);
  }

  function openAdd() {
    setEditing(null);
    setAdding(true);
    setForm({ name: '', code: '', decimalPlaces: '0', unitType: 'count' });
    setErrors({});
  }

  function openEdit(unit: Unit) {
    setAdding(false);
    setEditing(unit);
    setForm({
      name: unit.name,
      code: unit.code,
      decimalPlaces: String(unit.decimalPlaces),
      unitType: unit.unitType,
    });
    setErrors({});
  }

  function closeModal() {
    setAdding(false);
    setEditing(null);
    setErrors({});
  }

  function saveForm() {
    const decimalPlaces =
      form.decimalPlaces.trim() === '' ? Number.NaN : Number(form.decimalPlaces);
    const values = {
      name: form.name,
      code: form.code,
      decimalPlaces,
      unitType: form.unitType,
    };
    const result = editing ? updateUnit(editing.id, values) : addUnit(selectedWarehouse.id, values);
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
          labelKey="warehouse.units.activeCount"
          value={formatNumber(activeCount)}
          icon={Ruler}
          tone="healthy"
        />
        <StatCard
          labelKey="warehouse.units.archivedCount"
          value={formatNumber(archivedCount)}
          icon={Archive}
          tone="neutral"
        />
        <StatCard
          labelKey="warehouse.units.usageCountKpi"
          value={formatNumber(usedCount)}
          icon={Ruler}
          tone="info"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.units.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={() => {
            setSearch('');
            setStatusFilter('');
          }}
          filters={
            <select
              value={statusFilter}
              onChange={(event) => setStatusFilter(event.target.value as UnitStatusFilter)}
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
          rowKey={(row) => row.unit.id}
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
              {t('warehouse.units.savePreview')}
            </button>
          </>
        }
      >
        <div style={formGridStyle}>
          <FormField labelKey="warehouse.units.nameField" required errorKey={errorKey(errors.name)}>
            <input
              value={form.name}
              onChange={(event) => {
                setForm((current) => ({ ...current, name: event.target.value }));
                setErrors((current) => ({ ...current, name: undefined }));
              }}
            />
          </FormField>
          <FormField labelKey="warehouse.units.codeField" required errorKey={errorKey(errors.code)}>
            <input
              dir="ltr"
              value={form.code}
              onChange={(event) => {
                setForm((current) => ({ ...current, code: event.target.value }));
                setErrors((current) => ({ ...current, code: undefined }));
              }}
            />
          </FormField>
          <FormField labelKey="warehouse.units.typeField" errorKey={errorKey(errors.unitType)}>
            <select
              value={form.unitType}
              onChange={(event) => {
                setForm((current) => ({ ...current, unitType: event.target.value as UnitType }));
                setErrors((current) => ({ ...current, unitType: undefined }));
              }}
            >
              {unitTypeOptions.map((type) => (
                <option key={type} value={type}>
                  {t(unitTypeLabelKeys[type])}
                </option>
              ))}
            </select>
          </FormField>
          <FormField
            labelKey="warehouse.units.decimalsField"
            required
            errorKey={errorKey(errors.decimalPlaces)}
          >
            <input
              type="number"
              min="0"
              max="4"
              dir="ltr"
              value={form.decimalPlaces}
              onChange={(event) => {
                setForm((current) => ({ ...current, decimalPlaces: event.target.value }));
                setErrors((current) => ({ ...current, decimalPlaces: undefined }));
              }}
            />
          </FormField>
        </div>
      </Modal>

      <Modal
        open={details !== null}
        onClose={() => setDetails(null)}
        titleKey="warehouse.units.detailsTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={formGridStyle}>
            <div>
              <strong>{t('warehouse.units.nameField')}</strong>
              <p>{details.unit.name}</p>
            </div>
            <div>
              <strong>{t('warehouse.units.codeField')}</strong>
              <p dir="ltr">{details.unit.code}</p>
            </div>
            <div>
              <strong>{t('warehouse.units.typeField')}</strong>
              <p>{t(unitTypeLabelKeys[details.unit.unitType])}</p>
            </div>
            <div>
              <strong>{t('warehouse.units.decimalsColumn')}</strong>
              <p dir="ltr">{formatNumber(details.unit.decimalPlaces)}</p>
            </div>
            <div>
              <strong>{t('warehouse.units.usageCountColumn')}</strong>
              <p dir="ltr">{formatNumber(details.usageCount)}</p>
            </div>
            <div>
              <strong>{t('common.labels.status')}</strong>
              <p>
                <StatusBadge
                  tone={details.unit.isDeleted ? 'neutral' : 'healthy'}
                  labelKey={
                    details.unit.isDeleted ? 'common.status.archived' : 'common.status.active'
                  }
                />
              </p>
            </div>
            <div>
              <strong>{t('warehouse.productDetails.updatedLabel')}</strong>
              <p dir="ltr">{formatDateTime(details.unit.updatedAt)}</p>
            </div>
          </div>
        ) : null}
      </Modal>

      <ConfirmDialog
        open={archiveTarget !== null}
        onClose={() => setArchiveTarget(null)}
        onConfirm={() => {
          if (archiveTarget && archiveUnit(archiveTarget.id)) {
            showToast('warehouse.units.archiveToast', 'info');
          }
        }}
        titleKey="warehouse.units.archiveTitle"
        messageKey="warehouse.units.archiveMessage"
        confirmLabelKey="warehouse.products.archiveAction"
        tone="danger"
      />

      <Modal
        open={blockedUnit !== null}
        onClose={() => setBlockedUnit(null)}
        titleKey="warehouse.units.archiveBlockedTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
        footer={
          <button
            type="button"
            className="button button--primary"
            onClick={() => setBlockedUnit(null)}
          >
            {t('common.actions.close')}
          </button>
        }
      >
        <p className="confirm-dialog__message">
          {blockedUnit
            ? t('warehouse.units.archiveBlockedMessage', { count: formatNumber(blockedUnit.count) })
            : ''}
        </p>
      </Modal>
    </>
  );
}

export default UnitsPage;
