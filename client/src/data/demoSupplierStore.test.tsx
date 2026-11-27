import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import { demoProductDataset } from '@/data/demoData';
import {
  DemoProductStoreProvider,
  useDemoProductStore,
  type ProductFormValues,
} from '@/data/demoProductStore';
import {
  DemoSupplierStoreProvider,
  useDemoSupplierStore,
  type SupplierFormValues,
} from '@/data/demoSupplierStore';
import { getSupplierProductCount } from '@/lib/supplier';

function wrapper({ children }: { children: ReactNode }) {
  return (
    <DemoProductStoreProvider>
      <DemoSupplierStoreProvider>{children}</DemoSupplierStoreProvider>
    </DemoProductStoreProvider>
  );
}

function useStores() {
  return { supplier: useDemoSupplierStore(), product: useDemoProductStore() };
}

function supplierValues(overrides: Partial<SupplierFormValues> = {}): SupplierFormValues {
  return {
    name: 'مورد مضاف',
    code: 'sup-added',
    contactName: 'مسؤول المورد',
    phone: '+966 50 111 2222',
    email: 'added@example.invalid',
    address: 'الرياض',
    notes: 'ملاحظة محلية',
    ...overrides,
  };
}

function productValues(defaultSupplierId = ''): ProductFormValues {
  return {
    name: 'Supplier Test Product',
    nameAr: '',
    sku: 'SUP-SKU-1',
    description: '',
    categoryId: '',
    baseUnitId: 'unit-1',
    purchaseUnitId: '',
    isBatchTracked: false,
    isExpiryTracked: false,
    shelfLifeDays: null,
    defaultReasonCodeId: '',
    isActive: true,
    minimumQuantity: 1,
    maximumQuantity: null,
    reorderQuantity: null,
    defaultSupplierId,
    barcode: '',
    barcodeSymbology: 'Code128',
  };
}

describe('DemoSupplierStoreProvider', () => {
  it('initialises from the seed and derives usage counts from product settings', () => {
    const { result } = renderHook(useStores, { wrapper });
    const expected = getSupplierProductCount(
      demoProductDataset.products,
      demoProductDataset.settings,
      'sup-1',
    );
    const row = result.current.supplier
      .getSupplierSummaries('org-1')
      .find((item) => item.supplier.id === 'sup-1');

    expect(result.current.supplier.getSupplierById('sup-1')?.name).toBe('شركة الواحة للتجارة');
    expect(row?.usageCount).toBe(expected);
    expect(result.current.supplier.getUsageCount('sup-1')).toBe(expected);
    expect(result.current.supplier.getSupplierSummaries('org-2')).toHaveLength(1);
  });

  it('adds, edits, archives and restores suppliers with trimmed values', () => {
    const { result } = renderHook(useStores, { wrapper });

    act(() => {
      expect(
        result.current.supplier.addSupplier(
          'org-1',
          supplierValues({ name: '  مورد مضاف  ', code: '  sup-added  ' }),
        ),
      ).toBeNull();
    });
    const added = result.current.supplier.suppliers.find(
      (supplier) => supplier.code === 'SUP-ADDED',
    )!;
    expect(added.name).toBe('مورد مضاف');
    expect(added.organizationId).toBe('org-1');
    expect(added.isDeleted).toBe(false);

    act(() => {
      expect(
        result.current.supplier.updateSupplier(
          added.id,
          supplierValues({ name: ' مورد معدل ', code: ' sup-edited ' }),
        ),
      ).toBeNull();
    });
    expect(result.current.supplier.getSupplierById(added.id)?.name).toBe('مورد معدل');
    expect(result.current.supplier.getSupplierById(added.id)?.code).toBe('SUP-EDITED');

    act(() => {
      result.current.supplier.archiveSupplier(added.id);
    });
    expect(result.current.supplier.getSupplierById(added.id)?.isDeleted).toBe(true);
    expect(
      result.current.supplier
        .getSelectableSuppliers('org-1')
        .some((supplier) => supplier.id === added.id),
    ).toBe(false);

    act(() => {
      result.current.supplier.restoreSupplier(added.id);
    });
    expect(result.current.supplier.getSupplierById(added.id)?.isDeleted).toBe(false);
    expect(
      result.current.supplier
        .getSelectableSuppliers('org-1')
        .some((supplier) => supplier.id === added.id),
    ).toBe(true);
  });

  it('rejects duplicates within an organization but allows the same values in another one', () => {
    const { result } = renderHook(useStores, { wrapper });

    act(() => {
      expect(
        result.current.supplier.addSupplier(
          'org-1',
          supplierValues({ name: 'شركة الواحة للتجارة', code: 'UNIQUE' }),
        )?.name,
      ).toBe('duplicate');
      expect(
        result.current.supplier.addSupplier(
          'org-1',
          supplierValues({ name: 'مورد فريد', code: ' sup-waha ' }),
        )?.code,
      ).toBe('duplicate');
      expect(
        result.current.supplier.addSupplier(
          'org-2',
          supplierValues({ name: 'مورد فريد', code: 'SUP-WAHA' }),
        ),
      ).toBeNull();
    });
  });

  it('keeps product references when a supplier is archived and updates counts with products', () => {
    const { result } = renderHook(useStores, { wrapper });

    act(() => {
      result.current.supplier.addSupplier('org-1', supplierValues());
    });
    const supplier = result.current.supplier.suppliers.find((item) => item.code === 'SUP-ADDED')!;

    act(() => {
      result.current.product.addProduct('wh-1', productValues(supplier.id));
    });
    expect(result.current.supplier.getUsageCount(supplier.id)).toBe(1);

    act(() => {
      result.current.supplier.archiveSupplier(supplier.id);
    });
    const product = result.current.product.products.find((item) => item.sku === 'SUP-SKU-1')!;
    expect(
      result.current.product.settings.find((setting) => setting.productId === product.id)
        ?.defaultSupplierId,
    ).toBe(supplier.id);
    expect(result.current.supplier.getUsageCount(supplier.id)).toBe(1);
    expect(
      result.current.supplier
        .getSelectableSuppliers('org-1')
        .some((item) => item.id === supplier.id),
    ).toBe(false);
    expect(
      result.current.supplier
        .getSelectableSuppliers('org-1', [supplier.id])
        .some((item) => item.id === supplier.id),
    ).toBe(true);

    act(() => {
      result.current.product.archiveProduct(product.id);
    });
    expect(result.current.supplier.getUsageCount(supplier.id)).toBe(0);
  });
});
