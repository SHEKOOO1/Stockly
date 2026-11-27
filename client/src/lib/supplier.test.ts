import { describe, expect, it } from 'vitest';
import { demoProductDataset, demoProducts, demoSuppliers } from '@/data/demoData';
import {
  getSupplierDisplayName,
  getSupplierOptionLabel,
  getSupplierProductCount,
  hasSupplierValidationErrors,
  isSupplierSelectable,
  normalizeSupplierCode,
  validateSupplier,
} from '@/lib/supplier';

function validInput(overrides: Partial<Parameters<typeof validateSupplier>[0]> = {}) {
  return {
    name: 'مورد جديد',
    code: 'sup-new',
    contactName: 'مسؤول جديد',
    phone: '+966 50 123 4567',
    email: 'new@example.invalid',
    address: 'الرياض',
    notes: 'ملاحظات مختصرة',
    ...overrides,
  };
}

describe('supplier helpers', () => {
  it('normalises codes and validates required, length and duplicate values within the organization', () => {
    expect(normalizeSupplierCode('  sup-new  ')).toBe('SUP-NEW');

    const emptyErrors = validateSupplier(
      validInput({ name: '  ', code: ' ', contactName: ' ' }),
      demoSuppliers,
      'org-1',
    );
    expect(emptyErrors.name).toBe('required');
    expect(emptyErrors.code).toBe('required');
    expect(emptyErrors.contactName).toBe('required');

    const duplicateName = validateSupplier(
      validInput({ name: '  شركة الواحة للتجارة  ', code: 'UNIQUE' }),
      demoSuppliers,
      'org-1',
    );
    expect(duplicateName.name).toBe('duplicate');

    const duplicateCode = validateSupplier(
      validInput({ name: 'مورد فريد', code: ' sup-waha ' }),
      demoSuppliers,
      'org-1',
    );
    expect(duplicateCode.code).toBe('duplicate');

    const otherOrganization = validateSupplier(
      validInput({ code: 'SUP-WAHA' }),
      demoSuppliers,
      'org-2',
    );
    expect(otherOrganization.code).toBeUndefined();
    expect(hasSupplierValidationErrors(emptyErrors)).toBe(true);
  });

  it('validates optional phone and email formats and maximum lengths', () => {
    const invalid = validateSupplier(
      validInput({ phone: 'abc', email: 'not-an-email' }),
      demoSuppliers,
      'org-1',
    );
    expect(invalid.phone).toBe('invalid');
    expect(invalid.email).toBe('invalid');

    const tooLong = validateSupplier(
      validInput({ name: ' م '.repeat(70), address: 'ع'.repeat(251), notes: 'ن'.repeat(501) }),
      demoSuppliers,
      'org-1',
    );
    expect(tooLong.name).toBe('maxLength');
    expect(tooLong.address).toBe('maxLength');
    expect(tooLong.notes).toBe('maxLength');
  });

  it('derives distinct active-product usage counts from settings', () => {
    const products = [
      { id: 'p1', isDeleted: false },
      { id: 'p2', isDeleted: true },
    ];
    const settings = [
      { productId: 'p1', defaultSupplierId: 's1' },
      { productId: 'p1', defaultSupplierId: 's1' },
      { productId: 'p2', defaultSupplierId: 's1' },
    ];

    expect(getSupplierProductCount(products, settings, 's1')).toBe(1);
    expect(
      getSupplierProductCount(products, settings, 's1', { includeArchivedProducts: true }),
    ).toBe(2);
    expect(
      getSupplierProductCount(demoProducts, demoProductDataset.settings, 'sup-1'),
    ).toBeGreaterThan(0);
  });

  it('keeps archived suppliers displayable but not selectable', () => {
    expect(isSupplierSelectable(demoSuppliers[0])).toBe(true);
    expect(isSupplierSelectable(demoSuppliers[4])).toBe(false);
    expect(getSupplierDisplayName(undefined, 'بدون مورد')).toBe('بدون مورد');
    expect(getSupplierDisplayName(demoSuppliers[4], 'بدون مورد', '(مؤرشف)')).toBe(
      'ورشة الساحل (مؤرشف)',
    );
    expect(getSupplierOptionLabel(demoSuppliers[4], '(مؤرشف)')).toBe(
      'ورشة الساحل (SUP-COAST) (مؤرشف)',
    );
  });
});
