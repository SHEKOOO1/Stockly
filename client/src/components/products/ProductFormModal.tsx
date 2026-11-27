import { Barcode, Boxes, Layers3, Settings2 } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { FormField, Modal } from '@/components/ui';
import { useDemoCategoryStore } from '@/data/demoCategoryStore';
import { demoWarehouses } from '@/data/demoData';
import type { ProductFormValues } from '@/data/demoProductStore';
import { useDemoSupplierStore } from '@/data/demoSupplierStore';
import { useDemoUnitStore } from '@/data/demoUnitStore';
import { t, type TranslationKey } from '@/i18n';
import { getSupplierOptionLabel } from '@/lib/supplier';
import { getUnitOptionLabel } from '@/lib/unit';
import { getPrimaryBarcode, getWarehouseSetting, type ProductDataset } from '@/lib/product';
import type { BarcodeSymbology, Product } from '@/types/domain';

export interface ProductFormModalProps {
  open: boolean;
  product: Product | null;
  warehouseId: string;
  dataset: ProductDataset;
  titleKey?: TranslationKey;
  onClose: () => void;
  onSave: (values: ProductFormValues) => void;
}

type FormErrorKey =
  | 'name'
  | 'sku'
  | 'duplicate'
  | 'baseUnit'
  | 'shelfLife'
  | 'minimum'
  | 'initial'
  | 'maximum'
  | 'reorder';

const symbologies: readonly BarcodeSymbology[] = [
  'EAN13',
  'EAN8',
  'UPC-A',
  'Code128',
  'QR',
  'DataMatrix',
];

interface FormState {
  name: string;
  nameAr: string;
  sku: string;
  description: string;
  categoryId: string;
  baseUnitId: string;
  purchaseUnitId: string;
  isBatchTracked: boolean;
  isExpiryTracked: boolean;
  shelfLifeDays: string;
  defaultReasonCodeId: string;
  isActive: boolean;
  minimumQuantity: string;
  initialQuantity: string;
  maximumQuantity: string;
  reorderQuantity: string;
  defaultSupplierId: string;
  barcode: string;
  barcodeSymbology: BarcodeSymbology;
}

function emptyForm(): FormState {
  return {
    name: '',
    nameAr: '',
    sku: '',
    description: '',
    categoryId: '',
    baseUnitId: '',
    purchaseUnitId: '',
    isBatchTracked: false,
    isExpiryTracked: false,
    shelfLifeDays: '',
    defaultReasonCodeId: '',
    isActive: true,
    minimumQuantity: '0',
    initialQuantity: '0',
    maximumQuantity: '',
    reorderQuantity: '',
    defaultSupplierId: '',
    barcode: '',
    barcodeSymbology: 'EAN13',
  };
}

function formFromProduct(product: Product, dataset: ProductDataset): FormState {
  const barcode = getPrimaryBarcode(dataset, product);
  const setting = getWarehouseSetting(dataset, product);
  return {
    name: product.name,
    nameAr: product.nameAr ?? '',
    sku: product.sku,
    description: product.description ?? '',
    categoryId: product.categoryId ?? '',
    baseUnitId: product.baseUnitId,
    purchaseUnitId: product.purchaseUnitId ?? '',
    isBatchTracked: product.isBatchTracked,
    isExpiryTracked: product.isExpiryTracked,
    shelfLifeDays: product.shelfLifeDays === null ? '' : String(product.shelfLifeDays),
    defaultReasonCodeId: product.defaultReasonCodeId ?? '',
    isActive: product.isActive,
    minimumQuantity: setting?.minimumQuantity === undefined ? '0' : String(setting.minimumQuantity),
    initialQuantity: String(
      dataset.balances.find(
        (balance) =>
          balance.productId === product.id && balance.warehouseId === product.warehouseId,
      )?.onHandQuantity ?? 0,
    ),
    maximumQuantity:
      setting?.maximumQuantity === undefined || setting.maximumQuantity === null
        ? ''
        : String(setting.maximumQuantity),
    reorderQuantity:
      setting?.reorderQuantity === undefined || setting.reorderQuantity === null
        ? ''
        : String(setting.reorderQuantity),
    defaultSupplierId: setting?.defaultSupplierId ?? '',
    barcode: barcode?.barcode ?? '',
    barcodeSymbology: barcode?.symbology ?? 'EAN13',
  };
}

function parseOptionalInteger(value: string): number | null {
  if (value.trim() === '') {
    return null;
  }
  const parsed = Number(value);
  return Number.isInteger(parsed) ? parsed : Number.NaN;
}

export function ProductFormModal({
  open,
  product,
  warehouseId,
  dataset,
  titleKey,
  onClose,
  onSave,
}: ProductFormModalProps) {
  const [form, setForm] = useState<FormState>(emptyForm);
  const [errors, setErrors] = useState<Partial<Record<string, TranslationKey>>>({});

  useEffect(() => {
    if (!open) {
      return;
    }
    setForm(product ? formFromProduct(product, dataset) : emptyForm());
    setErrors({});
  }, [open, product, dataset, warehouseId]);

  const { categories: allCategories } = useDemoCategoryStore();
  const categories = useMemo(
    () =>
      allCategories.filter(
        (category) =>
          category.warehouseId === warehouseId &&
          (!category.isDeleted || category.id === form.categoryId),
      ),
    [allCategories, warehouseId, form.categoryId],
  );
  const { getSelectableUnits } = useDemoUnitStore();
  const units = useMemo(
    () => getSelectableUnits([product?.baseUnitId, product?.purchaseUnitId]),
    [getSelectableUnits, product?.baseUnitId, product?.purchaseUnitId],
  );
  const { getSelectableSuppliers } = useDemoSupplierStore();
  const organizationId = demoWarehouses.find(
    (warehouse) => warehouse.id === warehouseId,
  )?.organizationId;
  const suppliers = useMemo(
    () => getSelectableSuppliers(organizationId ?? '', [form.defaultSupplierId]),
    [getSelectableSuppliers, organizationId, form.defaultSupplierId],
  );

  function update<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
    setErrors((current) => ({ ...current, [key]: undefined }));
  }

  function validate(): FormErrorKey[] {
    const invalid: FormErrorKey[] = [];
    const nameLength = form.name.trim().length;
    const skuLength = form.sku.trim().length;
    const shelfLife = parseOptionalInteger(form.shelfLifeDays);
    const minimum = parseOptionalInteger(form.minimumQuantity);
    const initial = Number(form.initialQuantity);
    const maximum = parseOptionalInteger(form.maximumQuantity);
    const reorder = parseOptionalInteger(form.reorderQuantity);
    const normalizedName = form.name.trim().toLocaleLowerCase();
    const normalizedSku = form.sku.trim().toLocaleLowerCase();
    const duplicate = dataset.products.some(
      (item) =>
        item.id !== product?.id &&
        item.warehouseId === warehouseId &&
        !item.isDeleted &&
        (item.name.toLocaleLowerCase() === normalizedName ||
          item.sku.toLocaleLowerCase() === normalizedSku),
    );

    if (nameLength < 1 || nameLength > 200) {
      invalid.push('name');
    }
    if (skuLength < 3 || skuLength > 50) {
      invalid.push('sku');
    }
    if (duplicate) {
      invalid.push('duplicate');
    }
    if (form.baseUnitId === '') {
      invalid.push('baseUnit');
    }
    if (form.isExpiryTracked && (shelfLife === null || Number.isNaN(shelfLife) || shelfLife <= 0)) {
      invalid.push('shelfLife');
    }
    if (minimum === null || Number.isNaN(minimum) || minimum < 0) {
      invalid.push('minimum');
    }
    if (!Number.isInteger(initial) || initial < 0) {
      invalid.push('initial');
    }
    if (maximum !== null && (Number.isNaN(maximum) || maximum <= 0)) {
      invalid.push('maximum');
    }
    if (reorder !== null && (Number.isNaN(reorder) || reorder <= 0)) {
      invalid.push('reorder');
    }
    return invalid;
  }

  function save() {
    const invalid = validate();
    const nextErrors: Partial<Record<string, TranslationKey>> = {};
    for (const key of invalid) {
      const errorKeys: Record<FormErrorKey, TranslationKey> = {
        name: 'warehouse.products.nameLengthError',
        sku: 'warehouse.products.skuLengthError',
        duplicate: 'warehouse.products.duplicateError',
        baseUnit: 'warehouse.products.baseUnitRequired',
        shelfLife: 'warehouse.products.shelfLifeRequired',
        minimum: 'warehouse.products.minimumError',
        initial: 'warehouse.products.initialError',
        maximum: 'warehouse.products.maximumError',
        reorder: 'warehouse.products.reorderError',
      };
      nextErrors[key] = errorKeys[key];
    }
    setErrors(nextErrors);
    if (invalid.length > 0) {
      return;
    }

    onSave({
      name: form.name,
      nameAr: form.nameAr,
      sku: form.sku,
      description: form.description,
      categoryId: form.categoryId,
      baseUnitId: form.baseUnitId,
      purchaseUnitId: form.purchaseUnitId,
      isBatchTracked: form.isBatchTracked,
      isExpiryTracked: form.isExpiryTracked,
      shelfLifeDays: form.isExpiryTracked ? parseOptionalInteger(form.shelfLifeDays) : null,
      defaultReasonCodeId: form.defaultReasonCodeId,
      isActive: form.isActive,
      minimumQuantity: parseOptionalInteger(form.minimumQuantity) ?? 0,
      initialQuantity: Number(form.initialQuantity),
      maximumQuantity: parseOptionalInteger(form.maximumQuantity),
      reorderQuantity: parseOptionalInteger(form.reorderQuantity),
      defaultSupplierId: form.defaultSupplierId,
      barcode: form.barcode,
      barcodeSymbology: form.barcodeSymbology,
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      titleKey={
        titleKey ?? (product ? 'warehouse.products.editTitle' : 'warehouse.products.addTitle')
      }
      descriptionKey="warehouse.shared.formDescription"
      size="lg"
      footer={
        <>
          <button type="button" className="button button--quiet" onClick={onClose}>
            {t('common.actions.cancel')}
          </button>
          <button type="button" className="button button--primary" onClick={save}>
            {t('warehouse.shared.savePreview')}
          </button>
        </>
      }
    >
      <div className="product-form-grid">
        <section className="product-section product-form-grid">
          <h3 className="product-section__title">
            <Boxes size={16} aria-hidden="true" />
            {t('warehouse.products.identitySection')}
          </h3>
          <FormField
            labelKey="warehouse.products.nameField"
            required
            errorKey={errors.name ?? errors.duplicate}
          >
            <input value={form.name} onChange={(event) => update('name', event.target.value)} />
          </FormField>
          <FormField labelKey="warehouse.products.nameArField">
            <input value={form.nameAr} onChange={(event) => update('nameAr', event.target.value)} />
          </FormField>
          <FormField labelKey="warehouse.products.skuField" required errorKey={errors.sku}>
            <input
              dir="ltr"
              className="product-mono"
              value={form.sku}
              onChange={(event) => update('sku', event.target.value)}
            />
          </FormField>
          <FormField labelKey="warehouse.products.categoryField">
            <select
              value={form.categoryId}
              onChange={(event) => update('categoryId', event.target.value)}
            >
              <option value="">{t('warehouse.products.noCategory')}</option>
              {categories.map((category) => (
                <option
                  key={category.id}
                  value={category.id}
                  disabled={category.isDeleted && category.id !== form.categoryId}
                >
                  {category.name}
                  {category.isDeleted ? ` (${t('common.status.archived')})` : ''}
                </option>
              ))}
            </select>
          </FormField>
          <FormField labelKey="warehouse.products.descriptionField" className="form-field--full">
            <textarea
              rows={3}
              maxLength={1000}
              value={form.description}
              onChange={(event) => update('description', event.target.value)}
            />
          </FormField>
        </section>

        <section className="product-section product-form-grid">
          <h3 className="product-section__title">
            <Settings2 size={16} aria-hidden="true" />
            {t('warehouse.products.unitsSection')}
          </h3>
          <FormField
            labelKey="warehouse.products.baseUnitField"
            required
            errorKey={errors.baseUnit}
          >
            <select
              value={form.baseUnitId}
              onChange={(event) => update('baseUnitId', event.target.value)}
            >
              <option value="">{t('warehouse.products.chooseUnit')}</option>
              {units.map((unit) => (
                <option
                  key={unit.id}
                  value={unit.id}
                  disabled={unit.isDeleted && unit.id !== form.baseUnitId}
                >
                  {getUnitOptionLabel(unit, t('warehouse.shared.archivedUnitSuffix'))}
                </option>
              ))}
            </select>
          </FormField>
          <FormField labelKey="warehouse.products.purchaseUnitField">
            <select
              value={form.purchaseUnitId}
              onChange={(event) => update('purchaseUnitId', event.target.value)}
            >
              <option value="">{t('warehouse.products.chooseUnit')}</option>
              {units.map((unit) => (
                <option
                  key={unit.id}
                  value={unit.id}
                  disabled={unit.isDeleted && unit.id !== form.purchaseUnitId}
                >
                  {getUnitOptionLabel(unit, t('warehouse.shared.archivedUnitSuffix'))}
                </option>
              ))}
            </select>
          </FormField>
          <FormField
            labelKey="warehouse.products.minimumQuantityField"
            required
            errorKey={errors.minimum}
          >
            <input
              type="number"
              min="0"
              dir="ltr"
              value={form.minimumQuantity}
              onChange={(event) => update('minimumQuantity', event.target.value)}
            />
          </FormField>
          <FormField
            labelKey="warehouse.products.initialQuantityField"
            required
            errorKey={errors.initial}
          >
            <input
              type="number"
              min="0"
              dir="ltr"
              value={form.initialQuantity}
              onChange={(event) => update('initialQuantity', event.target.value)}
            />
          </FormField>
          <FormField labelKey="warehouse.products.maximumQuantityField" errorKey={errors.maximum}>
            <input
              type="number"
              min="1"
              dir="ltr"
              value={form.maximumQuantity}
              onChange={(event) => update('maximumQuantity', event.target.value)}
            />
          </FormField>
          <FormField labelKey="warehouse.products.reorderQuantityField" errorKey={errors.reorder}>
            <input
              type="number"
              min="1"
              dir="ltr"
              value={form.reorderQuantity}
              onChange={(event) => update('reorderQuantity', event.target.value)}
            />
          </FormField>
          <FormField labelKey="warehouse.products.supplierField">
            <select
              value={form.defaultSupplierId}
              onChange={(event) => update('defaultSupplierId', event.target.value)}
            >
              <option value="">{t('warehouse.products.noSupplier')}</option>
              {suppliers.map((supplier) => (
                <option key={supplier.id} value={supplier.id}>
                  {getSupplierOptionLabel(supplier, t('warehouse.suppliers.archivedSuffix'))}
                </option>
              ))}
            </select>
          </FormField>
        </section>

        <section className="product-section product-form-grid">
          <h3 className="product-section__title">
            <Layers3 size={16} aria-hidden="true" />
            {t('warehouse.products.trackingSection')}
          </h3>
          <FormField labelKey="warehouse.products.batchTrackedField">
            <label className="product-checkbox-field">
              <input
                type="checkbox"
                checked={form.isBatchTracked}
                onChange={(event) => update('isBatchTracked', event.target.checked)}
              />
            </label>
          </FormField>
          <FormField labelKey="warehouse.products.expiryTrackedField">
            <label className="product-checkbox-field">
              <input
                type="checkbox"
                checked={form.isExpiryTracked}
                onChange={(event) => update('isExpiryTracked', event.target.checked)}
              />
            </label>
          </FormField>
          <FormField labelKey="warehouse.products.shelfLifeField" errorKey={errors.shelfLife}>
            <input
              type="number"
              min="1"
              dir="ltr"
              disabled={!form.isExpiryTracked}
              value={form.shelfLifeDays}
              onChange={(event) => update('shelfLifeDays', event.target.value)}
            />
          </FormField>
          <FormField labelKey="warehouse.products.defaultReasonField">
            <select
              value={form.defaultReasonCodeId}
              onChange={(event) => update('defaultReasonCodeId', event.target.value)}
            >
              <option value="">{t('warehouse.products.noReason')}</option>
              <option value="reason-count">{t('warehouse.products.reasonCount')}</option>
              <option value="reason-damage">{t('warehouse.products.reasonDamage')}</option>
              <option value="reason-correction">{t('warehouse.products.reasonCorrection')}</option>
            </select>
          </FormField>
          <FormField labelKey="warehouse.products.activeField">
            <label className="product-checkbox-field">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(event) => update('isActive', event.target.checked)}
              />
            </label>
          </FormField>
        </section>

        <section className="product-section product-form-grid">
          <h3 className="product-section__title">
            <Barcode size={16} aria-hidden="true" />
            {t('warehouse.products.barcodeSection')}
          </h3>
          <FormField
            labelKey="warehouse.products.barcodeField"
            hintKey="warehouse.products.barcodeHelp"
          >
            <input
              dir="ltr"
              className="product-mono"
              value={form.barcode}
              onChange={(event) => update('barcode', event.target.value)}
            />
          </FormField>
          <FormField labelKey="warehouse.products.symbologyField">
            <select
              value={form.barcodeSymbology}
              dir="ltr"
              onChange={(event) =>
                update('barcodeSymbology', event.target.value as BarcodeSymbology)
              }
            >
              {symbologies.map((symbology) => (
                <option key={symbology} value={symbology}>
                  {symbology}
                </option>
              ))}
            </select>
          </FormField>
        </section>
      </div>
    </Modal>
  );
}
