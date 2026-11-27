import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { useApp } from '@/context/AppContext';
import {
  demoDataOrigin,
  demoMovements,
  demoStocktakingSessions,
  demoWarehouses,
} from '@/data/demoData';
import { useDemoProductStore } from '@/data/demoProductStore';
import { useDemoUnitStore } from '@/data/demoUnitStore';
import type { TranslationKey } from '@/i18n';
import { getUnitOptionLabel } from '@/lib/unit';
import type {
  EntityStatus,
  InventoryMovement,
  MovementType,
  Product,
  ProductWarehouseSetting,
  StockBalance,
  StocktakingSession,
} from '@/types/domain';

export type InventoryOperationErrorCode =
  | 'invalidOperation'
  | 'productRequired'
  | 'invalidWarehouse'
  | 'invalidQuantity'
  | 'archivedProduct'
  | 'insufficientStock'
  | 'duplicateRequest';

export const inventoryErrorKeys: Record<InventoryOperationErrorCode, TranslationKey> = {
  invalidOperation: 'warehouse.inventory.invalidOperation',
  productRequired: 'warehouse.inventory.productRequired',
  invalidWarehouse: 'warehouse.inventory.invalidWarehouse',
  invalidQuantity: 'warehouse.inventory.invalidQuantity',
  archivedProduct: 'warehouse.inventory.archivedProduct',
  insufficientStock: 'warehouse.inventory.insufficientStock',
  duplicateRequest: 'warehouse.inventory.duplicateRequest',
};

export interface RecordMovementInput {
  requestId: string;
  warehouseId: string;
  productId: string;
  type: MovementType;
  quantity: number;
  onHandDelta: number;
  incomingDelta?: number;
  unitId: string;
  responsibleUserId: string;
  relatedTransaction: string;
  notes: string;
  status?: EntityStatus;
}

export interface RecordStockCountInput {
  requestId: string;
  warehouseId: string;
  productId: string;
  countedQuantity: number;
  responsibleUserId: string;
  notes: string;
}

export type InventoryOperationResult =
  { ok: true; movement: InventoryMovement } | { ok: false; errorCode: InventoryOperationErrorCode };

interface DemoInventoryState {
  movements: readonly InventoryMovement[];
  stocktakingSessions: readonly StocktakingSession[];
}

export interface DemoInventoryStoreValue {
  movements: readonly InventoryMovement[];
  stocktakingSessions: readonly StocktakingSession[];
  recordMovement: (input: RecordMovementInput) => InventoryOperationResult;
  recordStockCount: (input: RecordStockCountInput) => InventoryOperationResult;
  confirmMovement: (movementId: string) => void;
  confirmStocktakingSession: (sessionId: string) => void;
}

const DemoInventoryStoreContext = createContext<DemoInventoryStoreValue | null>(null);

export function makeInventoryRequestId(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

function lowStockKey(warehouseId: string, productId: string): string {
  return `low-stock:${warehouseId}:${productId}`;
}

function buildLowStockState(
  products: readonly Product[],
  balances: readonly StockBalance[],
  settings: readonly ProductWarehouseSetting[],
): Map<string, boolean> {
  const state = new Map<string, boolean>();
  for (const product of products) {
    if (product.isDeleted || !product.isActive) {
      continue;
    }
    const minimum =
      settings.find(
        (item) => item.productId === product.id && item.warehouseId === product.warehouseId,
      )?.minimumQuantity ?? 0;
    if (minimum <= 0) {
      continue;
    }
    const balance = balances.find(
      (item) => item.productId === product.id && item.warehouseId === product.warehouseId,
    );
    state.set(
      lowStockKey(product.warehouseId, product.id),
      (balance?.onHandQuantity ?? 0) <= minimum,
    );
  }
  return state;
}

export function DemoInventoryStoreProvider({ children }: { children: ReactNode }) {
  const { products, balances, settings, applyInventoryDelta } = useDemoProductStore();
  const { units } = useDemoUnitStore();
  const { raiseLowStockNotifications, resolveLowStockNotifications } = useApp();
  const [state, setState] = useState<DemoInventoryState>(() => ({
    movements: [...demoMovements],
    stocktakingSessions: [...demoStocktakingSessions],
  }));
  const movementCounter = useRef(0);
  const sessionCounter = useRef(0);
  const recordedRequests = useRef(new Set<string>());
  const lowStockStateRef = useRef(buildLowStockState(products, balances, settings));

  useEffect(() => {
    const nextState = buildLowStockState(products, balances, settings);
    for (const [key, wasLow] of lowStockStateRef.current) {
      if (!nextState.has(key) && wasLow) {
        resolveLowStockNotifications(key);
      }
    }
    for (const [key, isLow] of nextState) {
      const wasLow = lowStockStateRef.current.get(key) === true;
      if (isLow === wasLow) {
        continue;
      }
      const [warehouseFragment, productId] = key.slice('low-stock:'.length).split(':');
      const product = products.find((item) => item.id === productId);
      if (!product) {
        continue;
      }
      const balance = balances.find(
        (item) => item.productId === product.id && item.warehouseId === product.warehouseId,
      );
      const minimum =
        settings.find(
          (item) => item.productId === product.id && item.warehouseId === product.warehouseId,
        )?.minimumQuantity ?? 0;
      if (isLow) {
        const unit = units.find((item) => item.id === product.baseUnitId);
        raiseLowStockNotifications({
          lowStockKey: key,
          productName: product.nameAr ?? product.name,
          warehouseName:
            demoWarehouses.find((warehouse) => warehouse.id === product.warehouseId)?.name ??
            warehouseFragment,
          quantity: balance?.onHandQuantity ?? 0,
          minimum,
          unitLabel: unit ? getUnitOptionLabel(unit, '') : product.baseUnitId,
        });
      } else {
        resolveLowStockNotifications(key);
      }
    }
    lowStockStateRef.current = nextState;
  }, [
    balances,
    products,
    settings,
    units,
    raiseLowStockNotifications,
    resolveLowStockNotifications,
  ]);

  const recordMovement = useCallback(
    (input: RecordMovementInput): InventoryOperationResult => {
      const requestId = input.requestId.trim();
      if (requestId === '') {
        return { ok: false, errorCode: 'invalidOperation' };
      }
      if (recordedRequests.current.has(requestId)) {
        return { ok: false, errorCode: 'duplicateRequest' };
      }
      if (input.warehouseId.trim() === '' || input.productId.trim() === '') {
        return { ok: false, errorCode: 'productRequired' };
      }
      const product = products.find(
        (item) => item.id === input.productId && item.warehouseId === input.warehouseId,
      );
      if (!product) {
        return { ok: false, errorCode: 'invalidWarehouse' };
      }
      if (product.isDeleted || !product.isActive) {
        return { ok: false, errorCode: 'archivedProduct' };
      }
      const quantityValid =
        Number.isFinite(input.quantity) &&
        Number.isFinite(input.onHandDelta) &&
        (input.type === 'COUNT'
          ? Number.isInteger(input.quantity)
          : input.type === 'ADJUSTMENT'
            ? Number.isInteger(input.quantity) && input.quantity !== 0
            : input.quantity > 0);
      if (!quantityValid) {
        return { ok: false, errorCode: 'invalidQuantity' };
      }

      const occurredAt = new Date().toISOString();
      const applied = applyInventoryDelta({
        productId: input.productId,
        warehouseId: input.warehouseId,
        onHandDelta: input.onHandDelta,
        incomingDelta: input.incomingDelta,
        occurredAt,
      });
      if (!applied) {
        return { ok: false, errorCode: 'insufficientStock' };
      }

      recordedRequests.current.add(requestId);
      movementCounter.current += 1;
      const movement: InventoryMovement = {
        origin: demoDataOrigin,
        id: `mov-local-${movementCounter.current}`,
        warehouseId: input.warehouseId,
        productId: input.productId,
        type: input.type,
        quantity: input.quantity,
        unitId: input.unitId,
        occurredAt,
        responsibleUserId: input.responsibleUserId,
        relatedTransaction: input.relatedTransaction,
        status: input.status ?? 'posted',
        notes: input.notes,
      };
      setState((current) => ({
        ...current,
        movements: [movement, ...current.movements],
      }));
      return { ok: true, movement };
    },
    [applyInventoryDelta, products],
  );

  const recordStockCount = useCallback(
    (input: RecordStockCountInput): InventoryOperationResult => {
      const requestId = input.requestId.trim();
      if (requestId === '') {
        return { ok: false, errorCode: 'invalidOperation' };
      }
      if (recordedRequests.current.has(requestId)) {
        return { ok: false, errorCode: 'duplicateRequest' };
      }
      if (input.warehouseId.trim() === '' || input.productId.trim() === '') {
        return { ok: false, errorCode: 'productRequired' };
      }
      const product = products.find(
        (item) => item.id === input.productId && item.warehouseId === input.warehouseId,
      );
      if (!product) {
        return { ok: false, errorCode: 'invalidWarehouse' };
      }
      if (product.isDeleted || !product.isActive) {
        return { ok: false, errorCode: 'archivedProduct' };
      }
      if (!Number.isInteger(input.countedQuantity) || input.countedQuantity < 0) {
        return { ok: false, errorCode: 'invalidQuantity' };
      }

      const balance = balances.find(
        (item) => item.productId === input.productId && item.warehouseId === input.warehouseId,
      );
      const expectedQuantity = balance?.onHandQuantity ?? 0;
      const difference = input.countedQuantity - expectedQuantity;
      const result = recordMovement({
        requestId,
        warehouseId: input.warehouseId,
        productId: input.productId,
        type: 'COUNT',
        quantity: difference,
        onHandDelta: difference,
        unitId: product.baseUnitId,
        responsibleUserId: input.responsibleUserId,
        relatedTransaction: `CNT-LOCAL-${Date.now().toString().slice(-4)}`,
        notes: input.notes,
      });
      if (!result.ok) {
        return result;
      }

      sessionCounter.current += 1;
      const session: StocktakingSession = {
        origin: demoDataOrigin,
        id: `count-local-${sessionCounter.current}`,
        warehouseId: input.warehouseId,
        code: `CNT-LOCAL-${sessionCounter.current.toString().padStart(3, '0')}`,
        status: 'confirmed',
        startedAt: result.movement.occurredAt,
        expectedLines: 1,
        countedLines: 1,
        differenceLines: difference === 0 ? 0 : 1,
      };
      setState((current) => ({
        ...current,
        stocktakingSessions: [session, ...current.stocktakingSessions],
      }));
      return result;
    },
    [balances, products, recordMovement],
  );

  const confirmMovement = useCallback((movementId: string) => {
    setState((current) => ({
      ...current,
      movements: current.movements.map((movement) =>
        movement.id === movementId ? { ...movement, status: 'confirmed' } : movement,
      ),
    }));
  }, []);

  const confirmStocktakingSession = useCallback((sessionId: string) => {
    setState((current) => ({
      ...current,
      stocktakingSessions: current.stocktakingSessions.map((session) =>
        session.id === sessionId ? { ...session, status: 'confirmed' } : session,
      ),
    }));
  }, []);

  const value = useMemo<DemoInventoryStoreValue>(
    () => ({
      movements: state.movements,
      stocktakingSessions: state.stocktakingSessions,
      recordMovement,
      recordStockCount,
      confirmMovement,
      confirmStocktakingSession,
    }),
    [state, recordMovement, recordStockCount, confirmMovement, confirmStocktakingSession],
  );

  return (
    <DemoInventoryStoreContext.Provider value={value}>
      {children}
    </DemoInventoryStoreContext.Provider>
  );
}

export function useDemoInventoryStore(): DemoInventoryStoreValue {
  const value = useContext(DemoInventoryStoreContext);
  if (!value) {
    throw new Error('useDemoInventoryStore must be used within DemoInventoryStoreProvider');
  }
  return value;
}
