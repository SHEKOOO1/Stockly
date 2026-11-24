import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import { demoProductDataset } from '@/data/demoData';
import {
  DemoProductStoreProvider,
  useDemoProductStore,
  type ProductFormValues,
} from '@/data/demoProductStore';

function wrapper({ children }: { children: ReactNode }) {
  return <DemoProductStoreProvider>{children}</DemoProductStoreProvider>;
}

const values: ProductFormValues = {
  name: 'منتج اختباري',
  nameAr: '',
  sku: 'TEST-SKU-900',
  description: '',
  categoryId: '',
  baseUnitId: 'unit-1',
  purchaseUnitId: '',
  isBatchTracked: false,
  isExpiryTracked: false,
  shelfLifeDays: null,
  defaultReasonCodeId: '',
  isActive: true,
  minimumQuantity: 12,
  maximumQuantity: 80,
  reorderQuantity: 20,
  defaultSupplierId: '',
  barcode: '9876543210123',
  barcodeSymbology: 'Code128',
};

describe('DemoProductStoreProvider', () => {
  it('creates nested barcode, setting and balance records for a new product', () => {
    const { result } = renderHook(() => useDemoProductStore(), { wrapper });

    act(() => {
      result.current.addProduct('wh-1', values);
    });

    expect(result.current.products).toHaveLength(demoProductDataset.products.length + 1);
    const added = result.current.products[result.current.products.length - 1];
    expect(added).not.toHaveProperty('unitCost');
    expect(result.current.barcodes).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          productId: added.id,
          barcode: '9876543210123',
          symbology: 'Code128',
          isPrimary: true,
        }),
      ]),
    );
    expect(result.current.settings).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          productId: added.id,
          minimumQuantity: 12,
          maximumQuantity: 80,
          reorderQuantity: 20,
        }),
      ]),
    );
    expect(result.current.balances).toEqual(
      expect.arrayContaining([expect.objectContaining({ productId: added.id, onHandQuantity: 0 })]),
    );
    expect(
      result.current.productSummaries.find((summary) => summary.product.id === added.id),
    ).toBeDefined();
  });

  it('keeps an inactive new product separate from the archive state', () => {
    const { result } = renderHook(() => useDemoProductStore(), { wrapper });

    act(() => {
      result.current.addProduct('wh-1', { ...values, isActive: false });
    });

    const added = result.current.products[result.current.products.length - 1];
    expect(added).toEqual(expect.objectContaining({ isActive: false, isDeleted: false }));
    expect(
      result.current.productSummaries.find((summary) => summary.product.id === added.id),
    ).toBeDefined();
  });

  it('updates nested values and toggles archive state locally', () => {
    const { result } = renderHook(() => useDemoProductStore(), { wrapper });
    const original = result.current.products.find((product) => product.id === 'prod-1');
    expect(original).toBeDefined();

    act(() => {
      result.current.updateProduct('prod-1', {
        ...values,
        name: 'أرز بسمتي محدث',
        minimumQuantity: 15,
      });
    });

    const updated = result.current.products.find((product) => product.id === 'prod-1');
    expect(updated?.name).toBe('أرز بسمتي محدث');
    const summary = result.current.getProductSummary(updated!);
    expect(summary.minimumQuantity).toBe(15);
    expect(summary.barcode?.barcode).toBe('9876543210123');
    expect(
      result.current.productSummaries.find((item) => item.product.id === 'prod-1')?.product.name,
    ).toBe('أرز بسمتي محدث');

    act(() => {
      result.current.archiveProduct('prod-1');
    });
    expect(result.current.products.find((product) => product.id === 'prod-1')).toEqual(
      expect.objectContaining({ isActive: false, isDeleted: true }),
    );
    expect(
      result.current.productSummaries.find((item) => item.product.id === 'prod-1')?.product
        .isDeleted,
    ).toBe(true);

    act(() => {
      result.current.updateProduct('prod-1', { ...values, isActive: true });
    });
    expect(result.current.products.find((product) => product.id === 'prod-1')).toEqual(
      expect.objectContaining({ isActive: false, isDeleted: true }),
    );

    act(() => {
      result.current.restoreProduct('prod-1');
    });
    expect(result.current.products.find((product) => product.id === 'prod-1')).toEqual(
      expect.objectContaining({ isActive: true, isDeleted: false }),
    );
    expect(
      result.current.productSummaries.find((item) => item.product.id === 'prod-1')?.product
        .isDeleted,
    ).toBe(false);
  });
});
