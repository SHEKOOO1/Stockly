import { describe, expect, it } from 'vitest';
import {
  canArchiveUnit,
  getUnitDisplayName,
  getUnitOptionLabel,
  getUnitUsageCount,
  hasUnitValidationErrors,
  normalizeUnitCode,
  validateUnit,
} from '@/lib/unit';
import type { Unit, UnitType } from '@/types/domain';

function unit(id: string, name: string, code: string): Pick<Unit, 'id' | 'name' | 'code'> {
  return { id, name, code };
}

function values(overrides: Partial<Parameters<typeof validateUnit>[0]> = {}) {
  const base: Parameters<typeof validateUnit>[0] = {
    name: 'متر',
    code: ' M ',
    decimalPlaces: 2,
    unitType: 'length',
  };
  return { ...base, ...overrides };
}

describe('unit helpers', () => {
  it('normalizes symbols with trimming and upper casing', () => {
    expect(normalizeUnitCode(' kg ')).toBe('KG');
  });

  it('validates required fields, duplicates, decimals and type', () => {
    const units = [unit('unit-1', 'كيلوغرام', 'KG'), unit('unit-2', 'قطعة', 'PCS')];
    expect(validateUnit(values(), units)).toEqual({});
    expect(validateUnit(values({ name: ' ' }), units).name).toBe('required');
    expect(validateUnit(values({ name: 'ك' }), units).name).toBe('minLength');
    expect(validateUnit(values({ name: ' كيلوغرام ' }), units).name).toBe('duplicate');
    expect(validateUnit(values({ code: ' kg ' }), units).code).toBe('duplicate');
    expect(validateUnit(values({ decimalPlaces: Number.NaN }), units).decimalPlaces).toBe(
      'invalid',
    );
    expect(validateUnit(values({ decimalPlaces: 5 }), units).decimalPlaces).toBe('range');
    expect(validateUnit(values({ unitType: 'unknown' as UnitType }), units).unitType).toBe(
      'invalid',
    );
    expect(hasUnitValidationErrors(validateUnit(values(), units))).toBe(false);
  });

  it('allows an edit to keep its own name and symbol', () => {
    const units = [unit('unit-1', 'كيلوغرام', 'KG')];
    expect(
      validateUnit(values({ name: 'كيلوغرام', code: 'KG', editingId: 'unit-1' }), units),
    ).toEqual({});
  });

  it('counts base and purchase references once and excludes archived products', () => {
    const products = [
      { baseUnitId: 'unit-1', purchaseUnitId: 'unit-2', isDeleted: false },
      { baseUnitId: 'unit-2', purchaseUnitId: 'unit-1', isDeleted: false },
      { baseUnitId: 'unit-1', purchaseUnitId: null, isDeleted: true },
    ];
    expect(getUnitUsageCount(products, 'unit-1')).toBe(2);
    expect(getUnitUsageCount(products, 'unit-1', { includeArchivedProducts: true })).toBe(3);
    expect(canArchiveUnit(products, 'unit-1')).toBe(false);
    expect(canArchiveUnit(products, 'unit-9')).toBe(true);
  });

  it('formats display labels with archived state', () => {
    const active = { name: 'قطعة', code: 'PCS', isDeleted: false };
    const archived = { name: 'قطعة', code: 'PCS', isDeleted: true };
    expect(getUnitDisplayName(active, '—', '(مؤرشف)')).toBe('قطعة');
    expect(getUnitDisplayName(archived, '—', '(مؤرشف)')).toBe('قطعة (مؤرشف)');
    expect(getUnitDisplayName(undefined, '—')).toBe('—');
    expect(getUnitOptionLabel(archived, '(مؤرشف)')).toBe('قطعة (PCS) (مؤرشف)');
  });
});
