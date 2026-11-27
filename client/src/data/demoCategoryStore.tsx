import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { demoCategories, demoDataOrigin } from '@/data/demoData';
import { useDemoProductStore } from '@/data/demoProductStore';
import {
  flattenCategoryTree,
  getCategoryDescendantIds,
  hasCategoryValidationErrors,
  isCategorySelectable,
  normalizeCategoryCode,
  validateCategory,
  type CategoryValidationErrors,
} from '@/lib/category';
import type { Category } from '@/types/domain';

export interface CategoryFormValues {
  name: string;
  code: string;
  parentId: string;
}

export interface CategorySummaryRow {
  category: Category;
  depth: number;
  productCount: number;
}

export interface DemoCategoryStoreValue {
  categories: readonly Category[];
  categorySummaries: readonly CategorySummaryRow[];
  addCategory: (warehouseId: string, values: CategoryFormValues) => CategoryValidationErrors | null;
  updateCategory: (
    categoryId: string,
    values: CategoryFormValues,
  ) => CategoryValidationErrors | null;
  archiveCategory: (categoryId: string) => void;
  restoreCategory: (categoryId: string) => void;
  getCategoryById: (categoryId: string | null | undefined) => Category | undefined;
  getCategorySummaries: (warehouseId: string) => readonly CategorySummaryRow[];
  getSelectableCategories: (warehouseId: string, excludeId?: string | null) => readonly Category[];
}

const DemoCategoryStoreContext = createContext<DemoCategoryStoreValue | null>(null);

function normalizeParentId(parentId: string): string | null {
  const trimmed = parentId.trim();
  return trimmed === '' ? null : trimmed;
}

function createCategory(
  id: string,
  warehouseId: string,
  values: CategoryFormValues,
  now: string,
): Category {
  return {
    origin: demoDataOrigin,
    id,
    warehouseId,
    name: values.name.trim(),
    code: normalizeCategoryCode(values.code),
    parentId: normalizeParentId(values.parentId),
    productCount: 0,
    isActive: true,
    isDeleted: false,
    updatedAt: now,
  };
}

export function DemoCategoryStoreProvider({ children }: { children: ReactNode }) {
  const { products } = useDemoProductStore();
  const [categories, setCategories] = useState<readonly Category[]>(() => demoCategories);
  const counter = useRef(0);

  const makeId = useCallback((prefix: string) => {
    counter.current += 1;
    return `${prefix}-local-${counter.current}`;
  }, []);

  const categorySummaries = useMemo(
    () => flattenCategoryTree(categories as Category[], products),
    [categories, products],
  );

  const getCategoryById = useCallback(
    (categoryId: string | null | undefined) =>
      categoryId ? categories.find((category) => category.id === categoryId) : undefined,
    [categories],
  );

  const getCategorySummaries = useCallback(
    (warehouseId: string) =>
      flattenCategoryTree(
        categories.filter((category) => category.warehouseId === warehouseId),
        products.filter((product) => product.warehouseId === warehouseId),
      ),
    [categories, products],
  );

  const getSelectableCategories = useCallback(
    (warehouseId: string, excludeId: string | null = null) => {
      const warehouseCategories = categories.filter(
        (category) => category.warehouseId === warehouseId,
      );
      const excluded = excludeId
        ? getCategoryDescendantIds(warehouseCategories, excludeId)
        : new Set<string>();
      if (excludeId) {
        excluded.add(excludeId);
      }
      return warehouseCategories.filter(
        (category) => isCategorySelectable(category) && !excluded.has(category.id),
      );
    },
    [categories],
  );

  const addCategory = useCallback(
    (warehouseId: string, values: CategoryFormValues): CategoryValidationErrors | null => {
      const warehouseCategories = categories.filter(
        (category) => category.warehouseId === warehouseId,
      );
      const errors = validateCategory(
        { ...values, parentId: normalizeParentId(values.parentId) },
        warehouseCategories,
      );
      if (hasCategoryValidationErrors(errors)) {
        return errors;
      }
      const now = new Date().toISOString();
      setCategories((current) => [
        ...current,
        createCategory(makeId('cat'), warehouseId, values, now),
      ]);
      return null;
    },
    [categories, makeId],
  );

  const updateCategory = useCallback(
    (categoryId: string, values: CategoryFormValues): CategoryValidationErrors | null => {
      const existing = categories.find((category) => category.id === categoryId);
      if (!existing) {
        return { name: 'required' };
      }
      const warehouseCategories = categories.filter(
        (category) => category.warehouseId === existing.warehouseId,
      );
      const errors = validateCategory(
        { ...values, parentId: normalizeParentId(values.parentId), editingId: categoryId },
        warehouseCategories,
      );
      if (hasCategoryValidationErrors(errors)) {
        return errors;
      }
      const now = new Date().toISOString();
      setCategories((current) =>
        current.map((category) =>
          category.id === categoryId
            ? {
                ...category,
                name: values.name.trim(),
                code: normalizeCategoryCode(values.code),
                parentId: normalizeParentId(values.parentId),
                updatedAt: now,
              }
            : category,
        ),
      );
      return null;
    },
    [categories],
  );

  const archiveCategory = useCallback((categoryId: string) => {
    setCategories((current) =>
      current.map((category) =>
        category.id === categoryId
          ? {
              ...category,
              isActive: false,
              isDeleted: true,
              updatedAt: new Date().toISOString(),
            }
          : category,
      ),
    );
  }, []);

  const restoreCategory = useCallback((categoryId: string) => {
    setCategories((current) =>
      current.map((category) =>
        category.id === categoryId
          ? {
              ...category,
              isActive: true,
              isDeleted: false,
              updatedAt: new Date().toISOString(),
            }
          : category,
      ),
    );
  }, []);

  const value = useMemo<DemoCategoryStoreValue>(
    () => ({
      categories,
      categorySummaries,
      addCategory,
      updateCategory,
      archiveCategory,
      restoreCategory,
      getCategoryById,
      getCategorySummaries,
      getSelectableCategories,
    }),
    [
      categories,
      categorySummaries,
      addCategory,
      updateCategory,
      archiveCategory,
      restoreCategory,
      getCategoryById,
      getCategorySummaries,
      getSelectableCategories,
    ],
  );

  return (
    <DemoCategoryStoreContext.Provider value={value}>{children}</DemoCategoryStoreContext.Provider>
  );
}

export function useDemoCategoryStore(): DemoCategoryStoreValue {
  const value = useContext(DemoCategoryStoreContext);
  if (!value) {
    throw new Error('useDemoCategoryStore must be used within DemoCategoryStoreProvider');
  }
  return value;
}
