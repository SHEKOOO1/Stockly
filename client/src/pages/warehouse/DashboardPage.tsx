import { Boxes, CalendarClock, Package, TriangleAlert } from 'lucide-react';
import { useCallback, useMemo } from 'react';
import {
  BarChart,
  ChartCard,
  DataTable,
  DemoNotice,
  EmptyState,
  ErrorState,
  LoadingState,
  PageHeader,
  StatCard,
  StatusBadge,
  type DataTableColumn,
} from '@/components/ui';
import { useApp } from '@/context/AppContext';
import { useDemoInventoryStore } from '@/data/demoInventoryStore';
import { useDemoProductStore } from '@/data/demoProductStore';
import { useDemoUnitStore } from '@/data/demoUnitStore';
import { t, type TranslationKey } from '@/i18n';
import { formatDate, formatDateTime, formatNumber } from '@/lib/format';
import { getUnitOptionLabel } from '@/lib/unit';
import { useDemoLoader } from '@/lib/useDemoLoader';
import type { ProductSummary } from '@/lib/product';
import type { ChartPoint, InventoryMovement, MovementType, StatusTone } from '@/types/domain';

const statGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const panelGridStyle = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))',
  gap: '16px',
  marginBlockEnd: '20px',
} as const;

const movementLabels: Record<MovementType, TranslationKey> = {
  STOCK_IN: 'warehouse.movements.typeStockIn',
  STOCK_OUT: 'warehouse.movements.typeStockOut',
  TRANSFER: 'warehouse.movements.typeTransfer',
  ADJUSTMENT: 'warehouse.movements.typeAdjustment',
  COUNT: 'warehouse.movements.typeCount',
};

const movementTones: Record<MovementType, StatusTone> = {
  STOCK_IN: 'healthy',
  STOCK_OUT: 'info',
  TRANSFER: 'neutral',
  ADJUSTMENT: 'warning',
  COUNT: 'neutral',
};

export function WarehouseDashboardPage() {
  const { selectedWarehouse } = useApp();
  const { productSummaries, products: allProducts } = useDemoProductStore();
  const { movements: allMovements } = useDemoInventoryStore();
  const { units } = useDemoUnitStore();
  const productsLoader = useDemoLoader(productSummaries, { delayMs: 180 });
  const movementsLoader = useDemoLoader(allMovements, { delayMs: 220 });

  const products = useMemo<ProductSummary[]>(
    () =>
      productSummaries.filter((summary) => summary.product.warehouseId === selectedWarehouse.id),
    [productSummaries, selectedWarehouse.id],
  );

  const movements = useMemo(
    () => allMovements.filter((movement) => movement.warehouseId === selectedWarehouse.id),
    [allMovements, selectedWarehouse.id],
  );

  const incoming = useMemo(
    () => movements.filter((movement) => movement.type === 'STOCK_IN').slice(0, 5),
    [movements],
  );

  const outgoing = useMemo(
    () => movements.filter((movement) => movement.type === 'STOCK_OUT').slice(0, 5),
    [movements],
  );

  const unitLabel = useCallback(
    (unitId: string) => {
      const unit = units.find((item) => item.id === unitId);
      return unit ? getUnitOptionLabel(unit, t('warehouse.shared.archivedUnitSuffix')) : '';
    },
    [units],
  );

  const summary = useMemo<ChartPoint[]>(() => {
    const types: readonly MovementType[] = [
      'STOCK_IN',
      'STOCK_OUT',
      'TRANSFER',
      'ADJUSTMENT',
      'COUNT',
    ];
    return types.map((type) => ({
      id: type,
      label: t(movementLabels[type]),
      value: movements.filter((movement) => movement.type === type).length,
      tone: movementTones[type],
    }));
  }, [movements]);

  const recentColumns = useMemo<DataTableColumn<InventoryMovement>[]>(
    () => [
      {
        id: 'product',
        headerKey: 'common.labels.product',
        cell: (row) => (
          <strong>
            {allProducts.find((product) => product.id === row.productId)?.name ?? row.productId}
          </strong>
        ),
        sortValue: (row) =>
          allProducts.find((product) => product.id === row.productId)?.name ?? row.productId,
      },
      {
        id: 'quantity',
        headerKey: 'common.labels.quantity',
        cell: (row) => (
          <span dir="ltr">
            {formatNumber(row.quantity)} {unitLabel(row.unitId)}
          </span>
        ),
        sortValue: (row) => row.quantity,
        align: 'end',
        ltr: true,
      },
      {
        id: 'date',
        headerKey: 'warehouse.dashboard.dateColumn',
        cell: (row) => <span dir="ltr">{formatDateTime(row.occurredAt)}</span>,
        sortValue: (row) => row.occurredAt,
        ltr: true,
      },
      {
        id: 'transaction',
        headerKey: 'warehouse.dashboard.transactionColumn',
        cell: (row) => <span dir="ltr">{row.relatedTransaction}</span>,
        ltr: true,
      },
    ],
    [allProducts, unitLabel],
  );

  const isLoading = productsLoader.status === 'loading' || movementsLoader.status === 'loading';
  const hasError = productsLoader.status === 'error' || movementsLoader.status === 'error';

  return (
    <>
      <PageHeader
        titleKey="warehouse.dashboard.title"
        descriptionKey="warehouse.dashboard.description"
        eyebrowKey="common.context.warehouse"
        actions={
          <>
            <span className="status-badge status-badge--info">{selectedWarehouse.name}</span>
            <span className="demo-badge">{t('common.demo.previewOnly')}</span>
          </>
        }
      />
      <DemoNotice compact />

      <div style={statGridStyle}>
        <StatCard
          labelKey="warehouse.dashboard.totalProducts"
          value={formatNumber(products.length)}
          hintKey="warehouse.dashboard.productsHint"
          icon={Package}
          tone="info"
        />
        <StatCard
          labelKey="warehouse.dashboard.stockItems"
          value={formatNumber(products.filter((summary) => summary.onHandQuantity > 0).length)}
          hintKey="warehouse.dashboard.stockItemsHint"
          icon={Boxes}
          tone="healthy"
        />
        <StatCard
          labelKey="warehouse.dashboard.lowStock"
          value={formatNumber(products.filter((summary) => summary.stockStatus === 'low').length)}
          hintKey="warehouse.dashboard.lowHint"
          icon={TriangleAlert}
          tone="warning"
        />
        <StatCard
          labelKey="warehouse.dashboard.outOfStock"
          value={formatNumber(products.filter((summary) => summary.stockStatus === 'out').length)}
          hintKey="warehouse.dashboard.outHint"
          icon={TriangleAlert}
          tone="critical"
        />
        <StatCard
          labelKey="warehouse.dashboard.expiringProducts"
          value={formatNumber(
            products.filter((summary) => summary.stockStatus === 'expiring').length,
          )}
          hintKey="warehouse.dashboard.expiringHint"
          icon={CalendarClock}
          tone="warning"
        />
      </div>

      {hasError ? (
        <ErrorState
          onRetry={() => {
            productsLoader.reload();
            movementsLoader.reload();
          }}
        />
      ) : isLoading ? (
        <LoadingState />
      ) : (
        <>
          <div style={panelGridStyle}>
            <ChartCard
              titleKey="warehouse.dashboard.movementChart"
              subtitleKey="warehouse.dashboard.movementChartSubtitle"
            >
              {summary.length > 0 ? (
                <BarChart data={summary} labelKey="warehouse.dashboard.movementChart" />
              ) : (
                <EmptyState descriptionKey="warehouse.shared.emptyRows" />
              )}
            </ChartCard>

            <ChartCard
              titleKey="warehouse.dashboard.recentIncoming"
              subtitleKey="warehouse.dashboard.recentIncomingSubtitle"
            >
              <DataTable
                rows={incoming}
                columns={recentColumns}
                rowKey={(row) => row.id}
                pageSize={5}
                captionKey="warehouse.dashboard.recentIncoming"
                emptyMessageKey="warehouse.dashboard.noIncoming"
              />
            </ChartCard>
          </div>

          <ChartCard
            titleKey="warehouse.dashboard.recentOutgoing"
            subtitleKey="warehouse.dashboard.recentOutgoingSubtitle"
          >
            <DataTable
              rows={outgoing}
              columns={recentColumns}
              rowKey={(row) => row.id}
              pageSize={5}
              captionKey="warehouse.dashboard.recentOutgoing"
              emptyMessageKey="warehouse.dashboard.noOutgoing"
            />
          </ChartCard>
        </>
      )}

      <div style={{ marginBlockStart: '16px', display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
        <StatusBadge tone="healthy" labelKey="warehouse.movements.typeStockIn" />
        <StatusBadge tone="info" labelKey="warehouse.movements.typeStockOut" />
        <StatusBadge tone="neutral" labelKey="warehouse.movements.typeTransfer" />
        <StatusBadge tone="warning" labelKey="warehouse.movements.typeAdjustment" />
        <StatusBadge tone="neutral" labelKey="warehouse.movements.typeCount" />
        <span className="status-badge status-badge--neutral" dir="ltr">
          {formatDate(selectedWarehouse.lastActivityAt)}
        </span>
      </div>
    </>
  );
}

export default WarehouseDashboardPage;
