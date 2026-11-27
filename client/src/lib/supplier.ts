import type { Product, ProductWarehouseSetting, Supplier } from '@/types/domain';

export interface SupplierSummary {
  readonly supplier: Supplier;
  readonly usageCount: number;
}

export interface SupplierValidationInput {
  name: string;
  code: string;
  contactName: string;
  phone: string;
  email: string;
  address: string;
  notes: string;
  editingId?: string | null;
}

export interface SupplierValidationErrors {
  name?: 'required' | 'minLength' | 'maxLength' | 'duplicate';
  code?: 'required' | 'minLength' | 'maxLength' | 'duplicate';
  contactName?: 'required' | 'minLength' | 'maxLength';
  phone?: 'invalid' | 'maxLength';
  email?: 'invalid' | 'maxLength';
  address?: 'maxLength';
  notes?: 'maxLength';
}

const nameMinLength = 3;
const nameMaxLength = 120;
const codeMinLength = 2;
const codeMaxLength = 40;
const contactMinLength = 2;
const contactMaxLength = 80;
const phoneMaxLength = 40;
const emailMaxLength = 160;
const addressMaxLength = 250;
const notesMaxLength = 500;

const phonePattern = /^[+]?[\d\s()-]+$/;
const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function normalizeText(value: string): string {
  return value.trim().toLocaleLowerCase();
}

export function normalizeSupplierCode(value: string): string {
  return value.trim().toLocaleUpperCase();
}

function isValidPhone(value: string): boolean {
  if (value === '') {
    return true;
  }
  if (!phonePattern.test(value)) {
    return false;
  }
  const digits = value.replace(/\D/g, '').length;
  return digits >= 7 && digits <= 15;
}

function isValidEmail(value: string): boolean {
  return value === '' || emailPattern.test(value);
}

export function getSupplierProductCount(
  products: readonly Pick<Product, 'id' | 'isDeleted'>[],
  settings: readonly Pick<ProductWarehouseSetting, 'productId' | 'defaultSupplierId'>[],
  supplierId: string,
  options: { includeArchivedProducts?: boolean } = {},
): number {
  const includeArchivedProducts = options.includeArchivedProducts ?? false;
  const productIds = new Set(
    products
      .filter((product) => includeArchivedProducts || !product.isDeleted)
      .map((product) => product.id),
  );
  const referenced = settings
    .filter((setting) => setting.defaultSupplierId === supplierId)
    .map((setting) => setting.productId)
    .filter((productId) => productIds.has(productId));
  return new Set(referenced).size;
}

export function isSupplierSelectable(supplier: Pick<Supplier, 'isActive' | 'isDeleted'>): boolean {
  return supplier.isActive && !supplier.isDeleted;
}

export function getSupplierDisplayName(
  supplier: Pick<Supplier, 'name' | 'isDeleted'> | undefined,
  fallback: string,
  archivedSuffix = '',
): string {
  if (!supplier) {
    return fallback;
  }
  if (supplier.isDeleted && archivedSuffix) {
    return `${supplier.name} ${archivedSuffix}`;
  }
  return supplier.name;
}

export function getSupplierOptionLabel(
  supplier: Pick<Supplier, 'name' | 'code' | 'isDeleted'>,
  archivedSuffix = '',
): string {
  const suffix = supplier.isDeleted && archivedSuffix ? ` ${archivedSuffix}` : '';
  return `${supplier.name} (${supplier.code})${suffix}`;
}

export function validateSupplier(
  input: SupplierValidationInput,
  suppliers: readonly Pick<Supplier, 'id' | 'organizationId' | 'name' | 'code'>[],
  organizationId: string,
): SupplierValidationErrors {
  const errors: SupplierValidationErrors = {};
  const name = input.name.trim();
  const code = normalizeSupplierCode(input.code);
  const contactName = input.contactName.trim();
  const phone = input.phone.trim();
  const email = input.email.trim();
  const address = input.address.trim();
  const notes = input.notes.trim();
  const editingId = input.editingId ?? null;
  const organizationSuppliers = suppliers.filter(
    (supplier) => supplier.organizationId === organizationId,
  );
  const normalizedName = normalizeText(name);

  if (name.length === 0) {
    errors.name = 'required';
  } else if (name.length < nameMinLength) {
    errors.name = 'minLength';
  } else if (name.length > nameMaxLength) {
    errors.name = 'maxLength';
  } else if (
    organizationSuppliers.some(
      (supplier) => supplier.id !== editingId && normalizeText(supplier.name) === normalizedName,
    )
  ) {
    errors.name = 'duplicate';
  }

  if (code.length === 0) {
    errors.code = 'required';
  } else if (code.length < codeMinLength) {
    errors.code = 'minLength';
  } else if (code.length > codeMaxLength) {
    errors.code = 'maxLength';
  } else if (
    organizationSuppliers.some(
      (supplier) => supplier.id !== editingId && normalizeSupplierCode(supplier.code) === code,
    )
  ) {
    errors.code = 'duplicate';
  }

  if (contactName.length === 0) {
    errors.contactName = 'required';
  } else if (contactName.length < contactMinLength) {
    errors.contactName = 'minLength';
  } else if (contactName.length > contactMaxLength) {
    errors.contactName = 'maxLength';
  }

  if (phone.length > phoneMaxLength) {
    errors.phone = 'maxLength';
  } else if (!isValidPhone(phone)) {
    errors.phone = 'invalid';
  }

  if (email.length > emailMaxLength) {
    errors.email = 'maxLength';
  } else if (!isValidEmail(email)) {
    errors.email = 'invalid';
  }

  if (address.length > addressMaxLength) {
    errors.address = 'maxLength';
  }
  if (notes.length > notesMaxLength) {
    errors.notes = 'maxLength';
  }

  return errors;
}

export function hasSupplierValidationErrors(errors: SupplierValidationErrors): boolean {
  return Object.keys(errors).length > 0;
}
