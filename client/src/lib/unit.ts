import { type TranslationKey } from '@/i18n';
import type { Product, Unit, UnitType } from '@/types/domain';

export interface UnitSummary {
  readonly unit: Unit;
  readonly usageCount: number;
}

export interface UnitValidationInput {
  name: string;
  code: string;
  decimalPlaces: number;
  unitType: UnitType;
  editingId?: string | null;
}

export interface UnitValidationErrors {
  name?: 'required' | 'minLength' | 'duplicate';
  code?: 'required' | 'duplicate';
  decimalPlaces?: 'invalid' | 'range';
  unitType?: 'invalid';
}

export const unitTypeLabelKeys: Record<UnitType, TranslationKey> = {
  count: 'warehouse.units.typeCount',
  weight: 'warehouse.units.typeWeight',
  volume: 'warehouse.units.typeVolume',
  packaging: 'warehouse.units.typePackaging',
  length: 'warehouse.units.typeLength',
};

const unitTypes = new Set<UnitType>(['count', 'weight', 'volume', 'packaging', 'length']);

function normalizeText(value: string): string {
  return value.trim().toLocaleLowerCase();
}

export function normalizeUnitCode(value: string): string {
  return value.trim().toLocaleUpperCase();
}

export function getUnitUsageCount(
  products: readonly Pick<Product, 'baseUnitId' | 'purchaseUnitId' | 'isDeleted'>[],
  unitId: string,
  options: { includeArchivedProducts?: boolean } = {},
): number {
  const includeArchivedProducts = options.includeArchivedProducts ?? false;
  return products.filter(
    (product) =>
      (includeArchivedProducts || !product.isDeleted) &&
      (product.baseUnitId === unitId || product.purchaseUnitId === unitId),
  ).length;
}

export function canArchiveUnit(
  products: readonly Pick<Product, 'baseUnitId' | 'purchaseUnitId' | 'isDeleted'>[],
  unitId: string,
): boolean {
  return getUnitUsageCount(products, unitId) === 0;
}

export function isUnitSelectable(unit: Pick<Unit, 'isActive' | 'isDeleted'>): boolean {
  return unit.isActive && !unit.isDeleted;
}

export function getUnitDisplayName(
  unit: Pick<Unit, 'name' | 'isDeleted'> | undefined,
  fallback: string,
  archivedSuffix = '',
): string {
  if (!unit) {
    return fallback;
  }
  if (unit.isDeleted && archivedSuffix) {
    return `${unit.name} ${archivedSuffix}`;
  }
  return unit.name;
}

export function getUnitOptionLabel(
  unit: Pick<Unit, 'name' | 'code' | 'isDeleted'>,
  archivedSuffix = '',
): string {
  const suffix = unit.isDeleted && archivedSuffix ? ` ${archivedSuffix}` : '';
  return `${unit.name} (${unit.code})${suffix}`;
}

export function validateUnit(
  input: UnitValidationInput,
  units: readonly Pick<Unit, 'id' | 'name' | 'code'>[],
): UnitValidationErrors {
  const errors: UnitValidationErrors = {};
  const name = input.name.trim();
  const code = normalizeUnitCode(input.code);
  const editingId = input.editingId ?? null;
  const normalizedName = normalizeText(name);

  if (name.length === 0) {
    errors.name = 'required';
  } else if (name.length < 2) {
    errors.name = 'minLength';
  } else if (
    units.some((unit) => unit.id !== editingId && normalizeText(unit.name) === normalizedName)
  ) {
    errors.name = 'duplicate';
  }

  if (code.length === 0) {
    errors.code = 'required';
  } else if (units.some((unit) => unit.id !== editingId && normalizeUnitCode(unit.code) === code)) {
    errors.code = 'duplicate';
  }

  if (!Number.isInteger(input.decimalPlaces)) {
    errors.decimalPlaces = 'invalid';
  } else if (input.decimalPlaces < 0 || input.decimalPlaces > 4) {
    errors.decimalPlaces = 'range';
  }

  if (!unitTypes.has(input.unitType)) {
    errors.unitType = 'invalid';
  }

  return errors;
}

export function hasUnitValidationErrors(errors: UnitValidationErrors): boolean {
  return Object.keys(errors).length > 0;
}
