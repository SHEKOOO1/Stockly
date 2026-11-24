import { BellRing, Check, Eye } from 'lucide-react';
import { useMemo, useState } from 'react';
import {
  ConfirmDialog,
  DataTable,
  DemoNotice,
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
import { demoAlerts, demoProducts } from '@/data/demoData';
import { t, type TranslationKey } from '@/i18n';
import { formatDateTime, formatNumber } from '@/lib/format';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { DemoAlert, EntityStatus, StatusTone } from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))',
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
  flexWrap: 'wrap',
  gap: '4px',
  justifyContent: 'flex-end',
} as const;

const filterSelectStyle = { minWidth: 160 } as const;

const severityOptions: readonly StatusTone[] = [
  'critical',
  'warning',
  'healthy',
  'info',
  'neutral',
];

const toneLabels: Record<StatusTone, TranslationKey> = {
  healthy: 'common.status.healthy',
  warning: 'common.status.warning',
  critical: 'common.status.critical',
  neutral: 'common.status.neutral',
  info: 'common.status.info',
};

const statusOptions: readonly EntityStatus[] = ['active', 'pending', 'inactive'];

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

export function LowStockAlertsPage() {
  const { selectedWarehouse, showToast } = useApp();
  const loader = useDemoLoader(demoAlerts);
  const [localAlerts, setLocalAlerts] = useState<readonly DemoAlert[]>(demoAlerts);
  const [search, setSearch] = useState('');
  const [severityFilter, setSeverityFilter] = useState<StatusTone | ''>('');
  const [statusFilter, setStatusFilter] = useState<EntityStatus | ''>('');
  const [details, setDetails] = useState<DemoAlert | null>(null);
  const [acknowledging, setAcknowledging] = useState<DemoAlert | null>(null);

  const rows = useMemo(
    () =>
      localAlerts
        .filter((alert) => alert.warehouseId === selectedWarehouse.id)
        .sort((left, right) => right.detectedAt.localeCompare(left.detectedAt)),
    [localAlerts, selectedWarehouse.id],
  );

  const filteredRows = useMemo(() => {
    const needle = search.trim().toLocaleLowerCase();
    return rows.filter((alert) => {
      const productName =
        demoProducts.find((product) => product.id === alert.productId)?.name ?? '';
      const matchesSearch =
        needle === '' ||
        `${alert.title} ${alert.message} ${productName}`.toLocaleLowerCase().includes(needle);
      const matchesSeverity = severityFilter === '' || alert.severity === severityFilter;
      const matchesStatus = statusFilter === '' || alert.status === statusFilter;
      return matchesSearch && matchesSeverity && matchesStatus;
    });
  }, [rows, search, severityFilter, statusFilter]);

  const activeCount = useMemo(
    () => rows.filter((alert) => alert.status === 'active' || alert.status === 'pending').length,
    [rows],
  );

  const criticalCount = useMemo(
    () => rows.filter((alert) => alert.severity === 'critical').length,
    [rows],
  );

  const columns: DataTableColumn<DemoAlert>[] = [
    {
      id: 'title',
      headerKey: 'warehouse.alerts.titleColumn',
      cell: (row) => <strong>{row.title}</strong>,
      sortValue: (row) => row.title,
    },
    {
      id: 'product',
      headerKey: 'warehouse.alerts.productColumn',
      cell: (row) =>
        demoProducts.find((product) => product.id === row.productId)?.name ?? row.productId,
      sortValue: (row) => row.productId,
    },
    {
      id: 'severity',
      headerKey: 'warehouse.alerts.severityColumn',
      cell: (row) => <StatusBadge tone={row.severity} labelKey={toneLabels[row.severity]} />,
      sortValue: (row) => row.severity,
    },
    {
      id: 'detected',
      headerKey: 'warehouse.alerts.detectedColumn',
      cell: (row) => <span dir="ltr">{formatDateTime(row.detectedAt)}</span>,
      sortValue: (row) => row.detectedAt,
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'warehouse.alerts.statusColumn',
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
            onClick={() => setAcknowledging(row)}
            aria-label={t('warehouse.alerts.acknowledgeAction')}
          >
            <Check size={14} aria-hidden="true" />
            {t('warehouse.alerts.acknowledgeAction')}
          </button>
        </div>
      ),
    },
  ];

  function resetFilters() {
    setSearch('');
    setSeverityFilter('');
    setStatusFilter('');
  }

  function confirmAcknowledge() {
    if (!acknowledging) {
      return;
    }
    setLocalAlerts((current) =>
      current.map((alert) =>
        alert.id === acknowledging.id ? { ...alert, status: 'inactive' } : alert,
      ),
    );
    showToast('warehouse.shared.statusToast', 'info');
    setAcknowledging(null);
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.alerts.title"
        descriptionKey="warehouse.alerts.description"
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
          labelKey="warehouse.alerts.title"
          value={formatNumber(rows.length)}
          icon={BellRing}
          tone="info"
        />
        <StatCard
          labelKey="common.status.active"
          value={formatNumber(activeCount)}
          icon={BellRing}
          tone="warning"
        />
        <StatCard
          labelKey="common.status.critical"
          value={formatNumber(criticalCount)}
          icon={BellRing}
          tone="critical"
        />
      </div>

      <div style={{ marginBlockEnd: '16px' }}>
        <SearchFilterBar
          value={search}
          onChange={setSearch}
          placeholderKey="warehouse.alerts.searchPlaceholder"
          resultCount={filteredRows.length}
          onReset={resetFilters}
          filters={
            <>
              <select
                value={severityFilter}
                onChange={(event) => setSeverityFilter(event.target.value as StatusTone | '')}
                aria-label={t('warehouse.alerts.severityFilter')}
                style={filterSelectStyle}
              >
                <option value="">{t('common.labels.all')}</option>
                {severityOptions.map((severity) => (
                  <option key={severity} value={severity}>
                    {t(toneLabels[severity])}
                  </option>
                ))}
              </select>
              <select
                value={statusFilter}
                onChange={(event) => setStatusFilter(event.target.value as EntityStatus | '')}
                aria-label={t('warehouse.alerts.statusFilter')}
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
      ) : (
        <DataTable
          rows={filteredRows}
          columns={columns}
          rowKey={(row) => row.id}
          pageSize={7}
          captionKey="warehouse.alerts.title"
          emptyMessageKey="warehouse.shared.emptyRows"
        />
      )}

      <Modal
        open={details !== null}
        onClose={() => setDetails(null)}
        titleKey="warehouse.alerts.detailsTitle"
        descriptionKey="warehouse.shared.formDescription"
        size="sm"
      >
        {details ? (
          <div style={detailGridStyle}>
            <div>
              <strong>{t('warehouse.alerts.messageLabel')}</strong>
              <p>{details.message}</p>
            </div>
            <div>
              <strong>{t('warehouse.alerts.productColumn')}</strong>
              <p>
                {demoProducts.find((product) => product.id === details.productId)?.name ??
                  details.productId}
              </p>
            </div>
            <div>
              <strong>{t('warehouse.alerts.severityColumn')}</strong>
              <p>
                <StatusBadge tone={details.severity} labelKey={toneLabels[details.severity]} />
              </p>
            </div>
            <div>
              <strong>{t('warehouse.alerts.detectedColumn')}</strong>
              <p dir="ltr">{formatDateTime(details.detectedAt)}</p>
            </div>
          </div>
        ) : null}
      </Modal>

      <ConfirmDialog
        open={acknowledging !== null}
        onClose={() => setAcknowledging(null)}
        onConfirm={confirmAcknowledge}
        titleKey="warehouse.shared.confirmReviewTitle"
        messageKey="warehouse.shared.confirmReviewMessage"
      />
    </>
  );
}

export default LowStockAlertsPage;
