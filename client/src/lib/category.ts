import type { Category, Product } from '@/types/domain';

export interface CategoryDataset {
  readonly categories: readonly Category[];
  readonly products: readonly Product[];
}

export interface CategorySummary {
  readonly category: Category;
  readonly productCount: number;
  readonly depth: number;
}

export interface CategoryValidationInput {
  name: string;
  code: string;
  parentId: string | null;
  editingId?: string | null;
}

export interface CategoryValidationErrors {
  name?: 'required' | 'minLength' | 'duplicate';
  code?: 'required' | 'minLength' | 'duplicate';
  parentId?: 'self' | 'cycle';
}

function normalizeText(value: string): string {
  return value.trim().toLocaleLowerCase();
}

export function normalizeCategoryCode(value: string): string {
  return value.trim().toLocaleUpperCase();
}

export function getCategoryProductCount(
  products: readonly Pick<Product, 'categoryId' | 'isDeleted'>[],
  categoryId: string,
  options: { includeArchivedProducts?: boolean } = {},
): number {
  const includeArchivedProducts = options.includeArchivedProducts ?? false;
  return products.filter(
    (product) =>
      product.categoryId === categoryId && (includeArchivedProducts || !product.isDeleted),
  ).length;
}

export function isCategorySelectable(category: Pick<Category, 'isActive' | 'isDeleted'>): boolean {
  return category.isActive && !category.isDeleted;
}

export function getCategoryDescendantIds(
  categories: readonly Pick<Category, 'id' | 'parentId'>[],
  categoryId: string,
): Set<string> {
  const descendants = new Set<string>();
  const visit = (id: string) => {
    for (const category of categories) {
      if (category.parentId === id && !descendants.has(category.id)) {
        descendants.add(category.id);
        visit(category.id);
      }
    }
  };
  visit(categoryId);
  return descendants;
}

export function isInvalidCategoryParent(
  categories: readonly Pick<Category, 'id' | 'parentId'>[],
  categoryId: string,
  parentId: string | null,
): boolean {
  if (parentId === null) {
    return false;
  }
  if (parentId === categoryId) {
    return true;
  }
  return getCategoryDescendantIds(categories, categoryId).has(parentId);
}

export function validateCategory(
  input: CategoryValidationInput,
  categories: readonly Pick<Category, 'id' | 'name' | 'code' | 'parentId'>[],
): CategoryValidationErrors {
  const errors: CategoryValidationErrors = {};
  const name = input.name.trim();
  const code = input.code.trim();
  const editingId = input.editingId ?? null;
  const normalizedName = normalizeText(name);
  const normalizedCode = normalizeCategoryCode(code);

  if (name.length === 0) {
    errors.name = 'required';
  } else if (name.length < 3) {
    errors.name = 'minLength';
  } else if (
    categories.some(
      (category) => category.id !== editingId && normalizeText(category.name) === normalizedName,
    )
  ) {
    errors.name = 'duplicate';
  }

  if (code.length === 0) {
    errors.code = 'required';
  } else if (code.length < 2) {
    errors.code = 'minLength';
  } else if (
    categories.some(
      (category) =>
        category.id !== editingId && normalizeCategoryCode(category.code) === normalizedCode,
    )
  ) {
    errors.code = 'duplicate';
  }

  if (input.parentId) {
    if (editingId && input.parentId === editingId) {
      errors.parentId = 'self';
    } else if (editingId && isInvalidCategoryParent(categories, editingId, input.parentId)) {
      errors.parentId = 'cycle';
    }
  }

  return errors;
}

export function hasCategoryValidationErrors(errors: CategoryValidationErrors): boolean {
  return Object.keys(errors).length > 0;
}

function buildCategoryTree(categories: readonly Category[]): {
  ids: ReadonlySet<string>;
  childrenByParent: ReadonlyMap<string, readonly Category[]>;
} {
  const ids = new Set(categories.map((category) => category.id));
  const children = new Map<string, Category[]>();
  for (const category of categories) {
    const parentId = category.parentId;
    if (parentId === null || parentId === category.id || parentId === '' || !ids.has(parentId)) {
      continue;
    }
    const siblings = children.get(parentId) ?? [];
    siblings.push(category);
    children.set(parentId, siblings);
  }
  return { ids, childrenByParent: children };
}

export function flattenCategories(
  categories: readonly Category[],
  parentId: string | null = null,
  depth = 0,
  visited: ReadonlySet<string> = new Set<string>(),
): CategorySummary[] {
  const { ids, childrenByParent } = buildCategoryTree(categories);
  const seen = new Set(visited);
  const rows: CategorySummary[] = [];
  const append = (category: Category, currentDepth: number) => {
    if (seen.has(category.id)) {
      return;
    }
    seen.add(category.id);
    rows.push({ category, depth: currentDepth, productCount: category.productCount });
    for (const child of childrenByParent.get(category.id) ?? []) {
      append(child, currentDepth + 1);
    }
  };

  if (parentId === null) {
    for (const category of categories) {
      if (
        category.parentId === null ||
        category.parentId === category.id ||
        !ids.has(category.parentId)
      ) {
        append(category, depth);
      }
    }
    for (const category of categories) {
      append(category, depth);
    }
  } else {
    for (const category of childrenByParent.get(parentId) ?? []) {
      append(category, depth);
    }
  }
  return rows;
}

export function flattenCategoryTree(
  categories: readonly Category[],
  products: readonly Pick<Product, 'categoryId' | 'isDeleted'>[],
  options: { includeArchivedProducts?: boolean } = {},
): CategorySummary[] {
  const { ids, childrenByParent } = buildCategoryTree(categories);
  const seen = new Set<string>();
  const rows: CategorySummary[] = [];
  const append = (category: Category, depth: number) => {
    if (seen.has(category.id)) {
      return;
    }
    seen.add(category.id);
    rows.push({
      category,
      depth,
      productCount: getCategoryProductCount(products, category.id, options),
    });
    for (const child of childrenByParent.get(category.id) ?? []) {
      append(child, depth + 1);
    }
  };

  for (const category of categories) {
    if (
      category.parentId === null ||
      category.parentId === category.id ||
      !ids.has(category.parentId)
    ) {
      append(category, 0);
    }
  }
  for (const category of categories) {
    append(category, 0);
  }
  return rows;
}

export function getCategoryDisplayName(
  category: Pick<Category, 'name' | 'isDeleted'> | undefined,
  fallback: string,
  archivedSuffix = '',
): string {
  if (!category) {
    return fallback;
  }
  if (category.isDeleted && archivedSuffix) {
    return `${category.name} ${archivedSuffix}`;
  }
  return category.name;
}
