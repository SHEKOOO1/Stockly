import { BarChart3, FileText, Play } from 'lucide-react';
import { useMemo, useState } from 'react';
import {
  BarChart,
  ChartCard,
  DataTable,
  DemoNotice,
  EmptyState,
  PageHeader,
  StatCard,
  StatusBadge,
  type DataTableColumn,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { demoAlerts, demoMovements } from '@/data/demoData';
import { useDemoProductStore } from '@/data/demoProductStore';
import { t, type TranslationKey } from '@/i18n';
import { formatDate, formatNumber } from '@/lib/format';
import type { ChartPoint, StatusTone } from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const catalogueStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(230px, 1fr))',
  gap: '12px',
  marginBlockEnd: '20px',
} as const;

const filterGridStyle = {
  display: 'flex',
  flexWrap: 'wrap',
  gap: '10px',
  alignItems: 'center',
  marginBlockEnd: '20px',
} as const;

type ReportId = 'inventory' | 'movement' | 'levels' | 'expiry' | 'alerts';

interface ReportDefinition {
  id: ReportId;
  titleKey: TranslationKey;
  descriptionKey: TranslationKey;
  tone: StatusTone;
}

interface PreviewRow {
  id: string;
  label: string;
  value: string;
  tone: StatusTone;
}

const reportDefinitions: readonly ReportDefinition[] = [
  {
    id: 'inventory',
    titleKey: 'warehouse.reports.inventoryTitle',
    descriptionKey: 'warehouse.reports.inventoryDescription',
    tone: 'info',
  },
  {
    id: 'movement',
    titleKey: 'warehouse.reports.movementTitle',
    descriptionKey: 'warehouse.reports.movementDescription',
    tone: 'healthy',
  },
  {
    id: 'levels',
    titleKey: 'warehouse.reports.levelsTitle',
    descriptionKey: 'warehouse.reports.levelsDescription',
    tone: 'healthy',
  },
  {
    id: 'expiry',
    titleKey: 'warehouse.reports.expiryTitle',
    descriptionKey: 'warehouse.reports.expiryDescription',
    tone: 'warning',
  },
  {
    id: 'alerts',
    titleKey: 'warehouse.reports.alertsTitle',
    descriptionKey: 'warehouse.reports.alertsDescription',
    tone: 'critical',
  },
];

const toneLabels: Record<StatusTone, TranslationKey> = {
  healthy: 'common.status.healthy',
  warning: 'common.status.warning',
  critical: 'common.status.critical',
  neutral: 'common.status.neutral',
  info: 'common.status.info',
};

const movementLabels: Record<string, TranslationKey> = {
  STOCK_IN: 'warehouse.movements.typeStockIn',
  STOCK_OUT: 'warehouse.movements.typeStockOut',
  TRANSFER: 'warehouse.movements.typeTransfer',
  ADJUSTMENT: 'warehouse.movements.typeAdjustment',
  COUNT: 'warehouse.movements.typeCount',
};

export function ReportsPage() {
  const { selectedWarehouse, showToast } = useApp();
  const { productSummaries } = useDemoProductStore();
  const [selectedReport, setSelectedReport] = useState<ReportId>('inventory');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [generated, setGenerated] = useState(false);

  const definition = useMemo(
    () => reportDefinitions.find((report) => report.id === selectedReport) ?? reportDefinitions[0],
    [selectedReport],
  );

  const preview = useMemo<{ chart: ChartPoint[]; rows: PreviewRow[] }>(() => {
    const products = productSummaries.filter(
      (summary) => summary.product.warehouseId === selectedWarehouse.id,
    );
    const movements = demoMovements.filter(
      (movement) =>
        movement.warehouseId === selectedWarehouse.id &&
        (dateFrom === '' || movement.occurredAt.slice(0, 10) >= dateFrom) &&
        (dateTo === '' || movement.occurredAt.slice(0, 10) <= dateTo),
    );
    const alerts = demoAlerts.filter((alert) => alert.warehouseId === selectedWarehouse.id);

    if (selectedReport === 'movement') {
      const types = ['STOCK_IN', 'STOCK_OUT', 'TRANSFER', 'ADJUSTMENT', 'COUNT'];
      const chart = types.map((type) => ({
        id: type,
        label: t(movementLabels[type] ?? 'warehouse.movements.title'),
        value: movements.filter((movement) => movement.type === type).length,
        tone: 'info' as StatusTone,
      }));
      return {
        chart,
        rows: chart.map((point) => ({
          id: point.id,
          label: point.label,
          value: formatNumber(point.value),
          tone: 'info' as StatusTone,
        })),
      };
    }

    if (selectedReport === 'levels') {
      const chart = products.map((summary) => ({
        id: summary.product.id,
        label: summary.product.name,
        value: summary.onHandQuantity,
        tone: 'healthy' as StatusTone,
      }));
      return {
        chart,
        rows: chart.map((point) => ({
          id: point.id,
          label: point.label,
          value: formatNumber(point.value),
          tone: 'healthy' as StatusTone,
        })),
      };
    }

    if (selectedReport === 'expiry') {
      const tracked = products.filter((summary) => summary.nearestExpiry?.expiryDate);
      const chart = tracked.map((summary) => ({
        id: summary.product.id,
        label: summary.product.name,
        value: Math.max(
          0,
          Math.round(
            (new Date(summary.nearestExpiry?.expiryDate ?? '').getTime() - Date.now()) / 86_400_000,
          ),
        ),
        tone: 'warning' as StatusTone,
      }));
      return {
        chart,
        rows: chart.map((point) => ({
          id: point.id,
          label: point.label,
          value: formatNumber(point.value),
          tone: 'warning' as StatusTone,
        })),
      };
    }

    if (selectedReport === 'alerts') {
      const severities: StatusTone[] = ['critical', 'warning', 'healthy', 'info', 'neutral'];
      const chart = severities.map((severity) => ({
        id: severity,
        label: t(toneLabels[severity]),
        value: alerts.filter((alert) => alert.severity === severity).length,
        tone: severity,
      }));
      return {
        chart,
        rows: chart.map((point) => ({
          id: point.id,
          label: point.label,
          value: formatNumber(point.value),
          tone: point.tone,
        })),
      };
    }

    const chart = products.map((summary) => ({
      id: summary.product.id,
      label: summary.product.name,
      value: summary.onHandQuantity,
      tone: 'info' as StatusTone,
    }));
    return {
      chart,
      rows: chart.map((point, index) => ({
        id: point.id,
        label: point.label,
        value: formatNumber(point.value),
        tone:
          products[index]?.stockStatus === 'low'
            ? ('warning' as StatusTone)
            : ('info' as StatusTone),
      })),
    };
  }, [productSummaries, selectedReport, selectedWarehouse.id, dateFrom, dateTo]);

  const columns: DataTableColumn<PreviewRow>[] = [
    {
      id: 'label',
      headerKey: 'common.labels.name',
      cell: (row) => <strong>{row.label}</strong>,
      sortValue: (row) => row.label,
    },
    {
      id: 'value',
      headerKey: 'warehouse.reports.rowsColumn',
      cell: (row) => <span dir="ltr">{row.value}</span>,
      sortValue: (row) => row.value,
      align: 'end',
      ltr: true,
    },
    {
      id: 'status',
      headerKey: 'warehouse.reports.statusLabel',
      cell: (row) => <StatusBadge tone={row.tone} labelKey={toneLabels[row.tone]} />,
    },
  ];

  function selectReport(id: ReportId) {
    setSelectedReport(id);
    setGenerated(false);
  }

  function generatePreview() {
    setGenerated(true);
    showToast('warehouse.shared.reportToast', 'info');
  }

  return (
    <>
      <PageHeader
        titleKey="warehouse.reports.title"
        descriptionKey="warehouse.reports.description"
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
          labelKey="warehouse.reports.catalogueTitle"
          value={formatNumber(reportDefinitions.length)}
          icon={FileText}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.reports.previewTitle"
          value={
            generated ? t('warehouse.reports.generatedLocal') : t('common.states.notAvailable')
          }
          icon={BarChart3}
          tone={generated ? 'healthy' : 'neutral'}
        />
      </div>

      <ChartCard
        titleKey="warehouse.reports.catalogueTitle"
        subtitleKey="warehouse.reports.catalogueSubtitle"
      >
        <div style={catalogueStyle}>
          {reportDefinitions.map((report) => {
            const active = report.id === selectedReport;
            return (
              <button
                key={report.id}
                type="button"
                className={`stat-card stat-card--${report.tone} ${active ? 'is-active' : ''}`.trim()}
                onClick={() => selectReport(report.id)}
                aria-pressed={active}
              >
                <strong>{t(report.titleKey)}</strong>
                <span className="stat-card__hint">{t(report.descriptionKey)}</span>
              </button>
            );
          })}
        </div>
        <div style={filterGridStyle}>
          <input
            type="date"
            value={dateFrom}
            onChange={(event) => {
              setDateFrom(event.target.value);
              setGenerated(false);
            }}
            aria-label={t('warehouse.reports.dateFrom')}
            dir="ltr"
          />
          <input
            type="date"
            value={dateTo}
            onChange={(event) => {
              setDateTo(event.target.value);
              setGenerated(false);
            }}
            aria-label={t('warehouse.reports.dateTo')}
            dir="ltr"
          />
          <span className="status-badge status-badge--neutral" dir="ltr">
            {t('warehouse.reports.periodLabel')}:{' '}
            {dateFrom ? formatDate(dateFrom) : t('common.states.notAvailable')} —{' '}
            {dateTo ? formatDate(dateTo) : t('common.states.notAvailable')}
          </span>
          <button
            type="button"
            className="button button--primary button--sm"
            onClick={generatePreview}
          >
            <Play size={14} aria-hidden="true" />
            {t('warehouse.reports.generateAction')}
          </button>
        </div>
      </ChartCard>

      <ChartCard titleKey={definition.titleKey} subtitleKey="warehouse.reports.previewSubtitle">
        {!generated ? (
          <EmptyState descriptionKey="warehouse.reports.previewEmpty" />
        ) : preview.chart.length === 0 ? (
          <EmptyState descriptionKey="warehouse.shared.emptyRows" />
        ) : (
          <>
            <BarChart data={preview.chart} labelKey={definition.titleKey} />
            <div style={{ marginBlockStart: '16px' }}>
              <DataTable
                rows={preview.rows}
                columns={columns}
                rowKey={(row) => row.id}
                pageSize={5}
                captionKey={definition.titleKey}
                emptyMessageKey="warehouse.shared.emptyRows"
              />
            </div>
          </>
        )}
      </ChartCard>
    </>
  );
}

export default ReportsPage;
