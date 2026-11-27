import { describe, expect, it } from 'vitest';
import { demoCategories, demoProducts } from '@/data/demoData';
import {
  flattenCategoryTree,
  getCategoryDescendantIds,
  getCategoryDisplayName,
  getCategoryProductCount,
  isInvalidCategoryParent,
  normalizeCategoryCode,
  validateCategory,
} from '@/lib/category';

const warehouseCategories = demoCategories.filter((category) => category.warehouseId === 'wh-1');

describe('category helpers', () => {
  it('derives product counts from non-archived products', () => {
    expect(getCategoryProductCount(demoProducts, 'cat-1')).toBe(
      demoProducts.filter((product) => product.categoryId === 'cat-1' && !product.isDeleted).length,
    );
    expect(getCategoryProductCount(demoProducts, 'cat-1', { includeArchivedProducts: true })).toBe(
      demoProducts.filter((product) => product.categoryId === 'cat-1').length,
    );
  });

  it('requires a trimmed name and code', () => {
    const errors = validateCategory(
      { name: '   ', code: ' ', parentId: null },
      warehouseCategories,
    );
    expect(errors.name).toBe('required');
    expect(errors.code).toBe('required');
    expect(
      validateCategory(
        { name: '  New Name  ', code: '  nn  ', parentId: null },
        warehouseCategories,
      ),
    ).toEqual({});
    expect(normalizeCategoryCode('  nn  ')).toBe('NN');
  });

  it('prevents duplicate names and codes case-insensitively', () => {
    const errors = validateCategory(
      {
        name: warehouseCategories[0].name,
        code: warehouseCategories[0].code.toLowerCase(),
        parentId: null,
      },
      warehouseCategories,
    );
    expect(errors.name).toBe('duplicate');
    expect(errors.code).toBe('duplicate');
  });

  it('prevents self-parent and descendant cycles', () => {
    const categories = [
      ...warehouseCategories,
      {
        ...warehouseCategories[0],
        id: 'cat-child',
        parentId: 'cat-1',
        name: 'Child',
        code: 'CHILD',
      },
    ];
    expect(
      validateCategory(
        { name: 'New', code: 'NEW', parentId: 'cat-1', editingId: 'cat-1' },
        categories,
      ).parentId,
    ).toBe('self');
    expect(isInvalidCategoryParent(categories, 'cat-1', 'cat-child')).toBe(true);
    expect(getCategoryDescendantIds(categories, 'cat-1').has('cat-child')).toBe(true);
  });

  it('flattens hierarchy with derived counts and tolerates invalid demo data', () => {
    const rows = flattenCategoryTree(warehouseCategories, demoProducts);
    expect(rows.length).toBe(warehouseCategories.length);
    expect(rows.every((row) => typeof row.productCount === 'number')).toBe(true);

    const base = warehouseCategories[0];
    const invalid = [
      { ...base, id: 'orphan', parentId: 'missing-id', name: 'Orphan', code: 'ORPH' },
      { ...base, id: 'cycle-a', parentId: 'cycle-b', name: 'Cycle A', code: 'CYA' },
      { ...base, id: 'cycle-b', parentId: 'cycle-a', name: 'Cycle B', code: 'CYB' },
      { ...base, id: 'self-parent', parentId: 'self-parent', name: 'Self', code: 'SELF' },
    ];
    const invalidRows = flattenCategoryTree(invalid, []);
    expect(new Set(invalidRows.map((row) => row.category.id)).size).toBe(invalid.length);
    expect(invalidRows.every((row) => row.productCount === 0)).toBe(true);
  });

  it('formats archived category labels with a safe fallback', () => {
    const active = warehouseCategories[0];
    expect(getCategoryDisplayName(active, 'fallback', '(مؤرشف)')).toBe(active.name);
    expect(getCategoryDisplayName({ ...active, isDeleted: true }, 'fallback', '(مؤرشف)')).toBe(
      `${active.name} (مؤرشف)`,
    );
    expect(getCategoryDisplayName(undefined, 'fallback', '(مؤرشف)')).toBe('fallback');
  });
});
