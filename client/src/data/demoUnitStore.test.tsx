import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import { demoProducts } from '@/data/demoData';
import {
  DemoProductStoreProvider,
  useDemoProductStore,
  type ProductFormValues,
} from '@/data/demoProductStore';
import { DemoUnitStoreProvider, useDemoUnitStore } from '@/data/demoUnitStore';

function wrapper({ children }: { children: ReactNode }) {
  return (
    <DemoProductStoreProvider>
      <DemoUnitStoreProvider>{children}</DemoUnitStoreProvider>
    </DemoProductStoreProvider>
  );
}

function useStores() {
  return { unit: useDemoUnitStore(), prod: useDemoProductStore() };
}

function productValues(baseUnitId: string, purchaseUnitId = ''): ProductFormValues {
  return {
    name: 'Unit Test Product',
    nameAr: '',
    sku: 'UNIT-SKU-1',
    description: '',
    categoryId: '',
    baseUnitId,
    purchaseUnitId,
    isBatchTracked: false,
    isExpiryTracked: false,
    shelfLifeDays: null,
    defaultReasonCodeId: '',
    isActive: true,
    minimumQuantity: 1,
    maximumQuantity: null,
    reorderQuantity: null,
    defaultSupplierId: '',
    barcode: '',
    barcodeSymbology: 'Code128',
  };
}

describe('DemoUnitStoreProvider', () => {
  it('initialises from the seed and derives counts from products', () => {
    const { result } = renderHook(useStores, { wrapper });
    const expected = demoProducts.filter(
      (product) =>
        !product.isDeleted &&
        (product.baseUnitId === 'unit-1' || product.purchaseUnitId === 'unit-1'),
    ).length;
    const row = result.current.unit
      .getUnitSummaries('wh-1')
      .find((item) => item.unit.id === 'unit-1');

    expect(result.current.unit.getUnitById('unit-1')?.name).toBe('قطعة');
    expect(row?.usageCount).toBe(expected);
    expect(result.current.unit.getUsageCount('unit-1')).toBe(expected);
  });

  it('adds, edits, archives and restores units with trimmed values', () => {
    const { result } = renderHook(useStores, { wrapper });

    act(() => {
      expect(
        result.current.unit.addUnit('wh-1', {
          name: '  Test Unit  ',
          code: ' test ',
          decimalPlaces: 2,
          unitType: 'count',
        }),
      ).toBeNull();
    });
    const added = result.current.unit.units.find((unit) => unit.code === 'TEST')!;
    expect(added.name).toBe('Test Unit');
    expect(added.warehouseId).toBe('wh-1');

    act(() => {
      expect(
        result.current.unit.updateUnit(added.id, {
          name: ' Edited Unit ',
          code: ' test2 ',
          decimalPlaces: 3,
          unitType: 'weight',
        }),
      ).toBeNull();
    });
    expect(result.current.unit.getUnitById(added.id)?.name).toBe('Edited Unit');
    expect(result.current.unit.getUnitById(added.id)?.code).toBe('TEST2');
    expect(result.current.unit.getUnitById(added.id)?.unitType).toBe('weight');

    act(() => {
      expect(result.current.unit.archiveUnit(added.id)).toBe(true);
    });
    expect(result.current.unit.getUnitById(added.id)?.isDeleted).toBe(true);
    expect(result.current.unit.getSelectableUnits().some((unit) => unit.id === added.id)).toBe(
      false,
    );

    act(() => {
      result.current.unit.restoreUnit(added.id);
    });
    expect(result.current.unit.getUnitById(added.id)?.isDeleted).toBe(false);
    expect(result.current.unit.getSelectableUnits().some((unit) => unit.id === added.id)).toBe(
      true,
    );
  });

  it('rejects duplicate names and symbols but allows an edit to keep its own values', () => {
    const { result } = renderHook(useStores, { wrapper });

    act(() => {
      expect(
        result.current.unit.addUnit('wh-1', {
          name: '  كيلوغرام ',
          code: 'OTHER',
          decimalPlaces: 0,
          unitType: 'weight',
        })?.name,
      ).toBe('duplicate');
      expect(
        result.current.unit.addUnit('wh-1', {
          name: 'وحدة جديدة',
          code: ' kg ',
          decimalPlaces: 0,
          unitType: 'weight',
        })?.code,
      ).toBe('duplicate');
      expect(
        result.current.unit.updateUnit('unit-2', {
          name: 'كيلوغرام',
          code: 'KG',
          decimalPlaces: 3,
          unitType: 'weight',
        }),
      ).toBeNull();
    });
  });

  it('blocks archiving while an active product references the unit', () => {
    const { result } = renderHook(useStores, { wrapper });

    act(() => {
      result.current.unit.addUnit('wh-1', {
        name: 'وحدة محجوبة',
        code: 'LOCK',
        decimalPlaces: 0,
        unitType: 'count',
      });
    });
    const unit = result.current.unit.units.find((item) => item.code === 'LOCK')!;

    act(() => {
      result.current.prod.addProduct('wh-1', productValues(unit.id));
    });
    expect(result.current.unit.getUsageCount(unit.id)).toBe(1);

    const product = result.current.prod.products.find((item) => item.sku === 'UNIT-SKU-1')!;
    act(() => {
      expect(result.current.unit.archiveUnit(unit.id)).toBe(false);
    });
    act(() => {
      result.current.prod.archiveProduct(product.id);
    });
    act(() => {
      expect(result.current.unit.archiveUnit(unit.id)).toBe(true);
    });
    expect(result.current.unit.getUnitById(unit.id)?.isDeleted).toBe(true);
  });

  it('updates derived counts when products are added, reassigned and archived', () => {
    const { result } = renderHook(useStores, { wrapper });

    act(() => {
      result.current.unit.addUnit('wh-1', {
        name: 'وحدة العد',
        code: 'COUNTU',
        decimalPlaces: 0,
        unitType: 'count',
      });
      result.current.unit.addUnit('wh-1', {
        name: 'وحدة الشراء',
        code: 'PURCH',
        decimalPlaces: 0,
        unitType: 'count',
      });
    });
    const base = result.current.unit.units.find((item) => item.code === 'COUNTU')!;
    const purchase = result.current.unit.units.find((item) => item.code === 'PURCH')!;

    act(() => {
      result.current.prod.addProduct('wh-1', productValues(base.id, purchase.id));
    });
    expect(result.current.unit.getUsageCount(base.id)).toBe(1);
    expect(result.current.unit.getUsageCount(purchase.id)).toBe(1);

    const product = result.current.prod.products.find((item) => item.sku === 'UNIT-SKU-1')!;
    act(() => {
      result.current.prod.archiveProduct(product.id);
    });
    expect(result.current.unit.getUsageCount(base.id)).toBe(0);
    expect(result.current.unit.getUsageCount(purchase.id)).toBe(0);

    act(() => {
      result.current.prod.restoreProduct(product.id);
      result.current.prod.updateProduct('prod-1', productValues(base.id));
    });
    expect(result.current.unit.getUsageCount(base.id)).toBe(2);
  });
});
