import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import { AppProvider } from '@/context/AppContext';
import { demoMovements } from '@/data/demoData';
import { DemoInventoryStoreProvider, useDemoInventoryStore } from '@/data/demoInventoryStore';
import { DemoProductStoreProvider, useDemoProductStore } from '@/data/demoProductStore';
import { DemoUnitStoreProvider } from '@/data/demoUnitStore';

function wrapper({ children }: { children: ReactNode }) {
  return (
    <AppProvider>
      <DemoProductStoreProvider>
        <DemoUnitStoreProvider>
          <DemoInventoryStoreProvider>{children}</DemoInventoryStoreProvider>
        </DemoUnitStoreProvider>
      </DemoProductStoreProvider>
    </AppProvider>
  );
}

function setup() {
  const { result } = renderHook(
    () => ({
      inventory: useDemoInventoryStore(),
      products: useDemoProductStore(),
    }),
    { wrapper },
  );
  return result;
}

function balanceFor(result: ReturnType<typeof setup>, productId: string, warehouseId = 'wh-1') {
  return result.current.products.balances.find(
    (balance) => balance.productId === productId && balance.warehouseId === warehouseId,
  );
}

describe('DemoInventoryStoreProvider', () => {
  it('records a receipt, updates the shared balance and creates one movement', () => {
    const result = setup();
    const before = balanceFor(result, 'prod-1');
    expect(before?.onHandQuantity).toBe(320);

    let ok = false;
    act(() => {
      ok = result.current.inventory.recordMovement({
        requestId: 'receive-1',
        warehouseId: 'wh-1',
        productId: 'prod-1',
        type: 'STOCK_IN',
        quantity: 5,
        onHandDelta: 5,
        unitId: 'unit-1',
        responsibleUserId: 'user-1',
        relatedTransaction: 'TXN-TEST-1',
        notes: 'اختبار استلام',
      }).ok;
    });

    expect(ok).toBe(true);
    expect(balanceFor(result, 'prod-1')?.onHandQuantity).toBe(325);
    expect(result.current.inventory.movements).toHaveLength(demoMovements.length + 1);
    expect(result.current.inventory.movements[0]).toEqual(
      expect.objectContaining({ type: 'STOCK_IN', quantity: 5, status: 'posted' }),
    );
  });

  it('rejects issuing more than the available balance without creating a movement', () => {
    const result = setup();
    expect(balanceFor(result, 'prod-4')?.onHandQuantity).toBe(0);

    let errorCode = '';
    act(() => {
      const outcome = result.current.inventory.recordMovement({
        requestId: 'issue-1',
        warehouseId: 'wh-1',
        productId: 'prod-4',
        type: 'STOCK_OUT',
        quantity: 1,
        onHandDelta: -1,
        unitId: 'unit-1',
        responsibleUserId: 'user-1',
        relatedTransaction: 'TXN-TEST-2',
        notes: 'اختبار صرف',
      });
      if (!outcome.ok) {
        errorCode = outcome.errorCode;
      }
    });

    expect(errorCode).toBe('insufficientStock');
    expect(balanceFor(result, 'prod-4')?.onHandQuantity).toBe(0);
    expect(result.current.inventory.movements).toHaveLength(demoMovements.length);
  });

  it('applies a signed adjustment difference with its reason and creates a movement', () => {
    const result = setup();

    act(() => {
      result.current.inventory.recordMovement({
        requestId: 'adjust-1',
        warehouseId: 'wh-1',
        productId: 'prod-1',
        type: 'ADJUSTMENT',
        quantity: -6,
        onHandDelta: -6,
        unitId: 'unit-1',
        responsibleUserId: 'user-1',
        relatedTransaction: 'ADJ-TEST-1',
        notes: 'DAMAGE:تلف في المعاينة',
        status: 'posted',
      });
    });

    expect(balanceFor(result, 'prod-1')?.onHandQuantity).toBe(314);
    expect(result.current.inventory.movements[0]).toEqual(
      expect.objectContaining({
        type: 'ADJUSTMENT',
        quantity: -6,
        notes: 'DAMAGE:تلف في المعاينة',
      }),
    );
  });

  it('refuses new operations for archived products', () => {
    const result = setup();

    act(() => {
      result.current.products.archiveProduct('prod-1');
    });

    let errorCode = '';
    act(() => {
      const outcome = result.current.inventory.recordMovement({
        requestId: 'receive-archived',
        warehouseId: 'wh-1',
        productId: 'prod-1',
        type: 'STOCK_IN',
        quantity: 1,
        onHandDelta: 1,
        unitId: 'unit-1',
        responsibleUserId: 'user-1',
        relatedTransaction: 'TXN-TEST-3',
        notes: 'منتج مؤرشف',
      });
      if (!outcome.ok) {
        errorCode = outcome.errorCode;
      }
    });

    expect(errorCode).toBe('archivedProduct');
    expect(result.current.inventory.movements).toHaveLength(demoMovements.length);
    expect(balanceFor(result, 'prod-1')?.onHandQuantity).toBe(320);
  });

  it('blocks duplicate submissions with the same request id', () => {
    const result = setup();
    const input = {
      requestId: 'receive-duplicate',
      warehouseId: 'wh-1',
      productId: 'prod-2',
      type: 'STOCK_IN' as const,
      quantity: 2,
      onHandDelta: 2,
      unitId: 'unit-1',
      responsibleUserId: 'user-1',
      relatedTransaction: 'TXN-TEST-4',
      notes: 'تكرار',
    };

    let firstOk = false;
    let secondError = '';
    act(() => {
      firstOk = result.current.inventory.recordMovement(input).ok;
      const outcome = result.current.inventory.recordMovement(input);
      if (!outcome.ok) {
        secondError = outcome.errorCode;
      }
    });

    expect(firstOk).toBe(true);
    expect(secondError).toBe('duplicateRequest');
    expect(balanceFor(result, 'prod-2')?.onHandQuantity).toBe(44);
    expect(result.current.inventory.movements).toHaveLength(demoMovements.length + 1);
  });

  it('reconciles a stock count into one movement, one session and the shared balance', () => {
    const result = setup();
    const beforeSessions = result.current.inventory.stocktakingSessions.length;

    act(() => {
      result.current.inventory.recordStockCount({
        requestId: 'count-1',
        warehouseId: 'wh-1',
        productId: 'prod-1',
        countedQuantity: 310,
        responsibleUserId: 'user-1',
        notes: 'جرد اختباري',
      });
    });

    expect(balanceFor(result, 'prod-1')?.onHandQuantity).toBe(310);
    expect(result.current.inventory.movements[0]).toEqual(
      expect.objectContaining({ type: 'COUNT', quantity: -10, status: 'posted' }),
    );
    expect(result.current.inventory.stocktakingSessions).toHaveLength(beforeSessions + 1);
    expect(result.current.inventory.stocktakingSessions[0]).toEqual(
      expect.objectContaining({
        status: 'confirmed',
        expectedLines: 1,
        countedLines: 1,
        differenceLines: 1,
      }),
    );
  });
});
