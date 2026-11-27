import { act, renderHook } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it } from 'vitest';
import { DemoCategoryStoreProvider, useDemoCategoryStore } from '@/data/demoCategoryStore';
import { demoCategories, demoProducts } from '@/data/demoData';
import {
  DemoProductStoreProvider,
  useDemoProductStore,
  type ProductFormValues,
} from '@/data/demoProductStore';
import { getCategoryProductCount } from '@/lib/category';

function wrapper({ children }: { children: ReactNode }) {
  return (
    <DemoProductStoreProvider>
      <DemoCategoryStoreProvider>{children}</DemoCategoryStoreProvider>
    </DemoProductStoreProvider>
  );
}

function useStores() {
  return { cat: useDemoCategoryStore(), prod: useDemoProductStore() };
}

function productValues(categoryId: string): ProductFormValues {
  return {
    name: 'Category Test Product',
    nameAr: '',
    sku: 'CAT-SKU-1',
    description: '',
    categoryId,
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
    defaultSupplierId: '',
    barcode: '',
    barcodeSymbology: 'Code128',
  };
}

describe('DemoCategoryStoreProvider', () => {
  it('initialises from the seed and derives counts from products, not seed fields', () => {
    const { result } = renderHook(useStores, { wrapper });
    const seed = demoCategories.find((category) => category.id === 'cat-1')!;
    const row = result.current.cat
      .getCategorySummaries('wh-1')
      .find((item) => item.category.id === 'cat-1');

    expect(result.current.cat.getCategoryById('cat-1')?.name).toBe(seed.name);
    expect(row?.productCount).toBe(
      demoProducts.filter((product) => product.categoryId === 'cat-1' && !product.isDeleted).length,
    );
  });

  it('adds, edits, archives and restores categories with trimmed values', () => {
    const { result } = renderHook(useStores, { wrapper });

    act(() => {
      expect(
        result.current.cat.addCategory('wh-1', {
          name: '  Test Category  ',
          code: ' test ',
          parentId: '',
        }),
      ).toBeNull();
    });
    const added = result.current.cat.categories.find((category) => category.code === 'TEST')!;
    expect(added.name).toBe('Test Category');
    expect(added.parentId).toBeNull();

    act(() => {
      expect(
        result.current.cat.updateCategory(added.id, {
          name: ' Edited Category ',
          code: ' test2 ',
          parentId: '',
        }),
      ).toBeNull();
    });
    expect(result.current.cat.getCategoryById(added.id)?.name).toBe('Edited Category');
    expect(result.current.cat.getCategoryById(added.id)?.code).toBe('TEST2');

    act(() => {
      result.current.cat.archiveCategory(added.id);
    });
    expect(result.current.cat.getCategoryById(added.id)?.isDeleted).toBe(true);
    expect(
      result.current.cat
        .getSelectableCategories('wh-1')
        .some((category) => category.id === added.id),
    ).toBe(false);

    act(() => {
      result.current.cat.restoreCategory(added.id);
    });
    expect(result.current.cat.getCategoryById(added.id)?.isDeleted).toBe(false);
    expect(
      result.current.cat
        .getSelectableCategories('wh-1')
        .some((category) => category.id === added.id),
    ).toBe(true);
  });

  it('rejects duplicate names and codes but allows an edit to keep its own values', () => {
    const { result } = renderHook(useStores, { wrapper });

    act(() => {
      expect(
        result.current.cat.addCategory('wh-1', {
          name: 'Duplicate Name',
          code: 'DUP',
          parentId: '',
        }),
      ).toBeNull();
    });
    const added = result.current.cat.categories.find((category) => category.code === 'DUP')!;

    act(() => {
      expect(
        result.current.cat.addCategory('wh-1', {
          name: '  duplicate name ',
          code: 'OTHER',
          parentId: '',
        })?.name,
      ).toBe('duplicate');
      expect(
        result.current.cat.addCategory('wh-1', {
          name: 'Another Name',
          code: ' dup ',
          parentId: '',
        })?.code,
      ).toBe('duplicate');
      expect(
        result.current.cat.updateCategory(added.id, {
          name: 'Duplicate Name',
          code: 'DUP',
          parentId: '',
        }),
      ).toBeNull();
    });
  });

  it('blocks self-parent and descendant cycles', () => {
    const { result } = renderHook(useStores, { wrapper });
    const root = result.current.cat.getCategoryById('cat-1')!;

    act(() => {
      expect(
        result.current.cat.addCategory('wh-1', {
          name: 'Child Node',
          code: 'CHILD',
          parentId: root.id,
        }),
      ).toBeNull();
    });
    const child = result.current.cat.categories.find((category) => category.code === 'CHILD')!;

    act(() => {
      expect(
        result.current.cat.updateCategory(root.id, {
          name: root.name,
          code: root.code,
          parentId: root.id,
        })?.parentId,
      ).toBe('self');
      expect(
        result.current.cat.updateCategory(root.id, {
          name: root.name,
          code: root.code,
          parentId: child.id,
        })?.parentId,
      ).toBe('cycle');
    });

    expect(
      result.current.cat
        .getSelectableCategories('wh-1', root.id)
        .some((category) => category.id === child.id),
    ).toBe(false);
    expect(result.current.cat.getCategoryById(root.id)?.parentId).toBeNull();
  });

  it('updates derived counts when products are added, reassigned and archived', () => {
    const { result } = renderHook(useStores, { wrapper });
    const count = (categoryId: string) =>
      result.current.cat.getCategorySummaries('wh-1').find((row) => row.category.id === categoryId)
        ?.productCount ?? 0;
    const before = count('cat-1');

    act(() => {
      result.current.prod.addProduct('wh-1', productValues('cat-1'));
    });
    expect(count('cat-1')).toBe(before + 1);

    const added = result.current.prod.products.find((product) => product.sku === 'CAT-SKU-1')!;
    act(() => {
      result.current.prod.archiveProduct(added.id);
    });
    expect(count('cat-1')).toBe(before);

    act(() => {
      result.current.prod.restoreProduct(added.id);
      result.current.prod.updateProduct('prod-1', productValues('cat-2'));
    });
    expect(count('cat-1')).toBe(before);
    expect(count('cat-2')).toBe(getCategoryProductCount(result.current.prod.products, 'cat-2'));
  });
});
